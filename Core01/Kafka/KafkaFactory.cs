using Confluent.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Kafka;

public record KafkaSettings(string Topic, string BootstrapServers, string GroupId);

public sealed partial class KafkaFactory
{
    public static Task UnboundedParallelismAtLeastOnceClient<TKey, TValue>(
        KafkaSettings kafkaSettings,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor,
        CancellationToken cancellationToken)
        => AtLeastOnceClient(
            kafkaSettings,
            processor,
            DataflowBlockOptions.Unbounded,
            cancellationToken);

    public static async Task AtLeastOnceClient<TKey, TValue>(
        KafkaSettings kafkaSettings,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var configuration = AutoOffsetDisabledConfig(kafkaSettings);
        using var client = new ConsumerBuilder<TKey, TValue>(configuration).Build();
        string topic = kafkaSettings.Topic;
        client.Subscribe(topic);

        try
        {
            await ConsumeAsync(client, processor, maxDegreeOfParallelism, cancellationToken);
        }
        finally
        {
            client.Close();
        }
    }

    private static ConsumerConfig AutoOffsetDisabledConfig(KafkaSettings kafkaSettings)
        => new()
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            GroupId = kafkaSettings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoOffsetStore = false,
        };

    private static async Task ConsumeAsync<TKey, TValue>(
        IConsumer<TKey, TValue> consumer,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {

        var kafkaProcessor = new ProcessAndOffsetProcessor<TKey, TValue>(
            consumer,
            processor,
            maxDegreeOfParallelism,
            cancellationToken);

        await Task.WhenAll(
            ConsumeAndProcessAsync(consumer, kafkaProcessor, cancellationToken),
            kafkaProcessor.Completion);
    }

    private static async Task ConsumeAndProcessAsync<TKey, TValue>(
        IConsumer<TKey, TValue> consumer,
        ProcessAndOffsetProcessor<TKey, TValue> kafkaProcessor,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        while (cancellationToken.IsCancellationRequested is false)
        {
            try
            {
                var kafkaMessage = consumer.Consume(cancellationToken);
                if (kafkaProcessor.Enqueue(kafkaMessage) is false)
                {
                    return;
                }
            }
            catch (ConsumeException ex)
            {
                // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                if (ex.Error.IsFatal)
                {
                    kafkaProcessor.Complete();
                    throw;
                }
            }
            catch
            {
                kafkaProcessor.Complete();
                throw;
            }
        }
    }
}
