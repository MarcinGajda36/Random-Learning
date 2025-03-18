namespace MarcinGajda.Kafka;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Confluent.Kafka;

public record KafkaSettings(string Topic, string BootstrapServers, string GroupId);

public sealed partial class KafkaClient
{
    private const int MaxBufferedMessages = 4096;
    public static Task UnboundedParallelismAtLeastOnceClient<TKey, TValue>(
        KafkaSettings kafkaSettings,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
        int maxBufferedMessages = MaxBufferedMessages,
        CancellationToken cancellationToken = default)
        => AtLeastOnceClient(
            kafkaSettings,
            processor,
            DataflowBlockOptions.Unbounded,
            maxBufferedMessages,
            cancellationToken);

    public static async Task AtLeastOnceClient<TKey, TValue>(
        KafkaSettings kafkaSettings,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
        int maxDegreeOfParallelism = 1,
        int maxBufferedMessages = MaxBufferedMessages,
        CancellationToken cancellationToken = default)
    {
        var configuration = AutoOffsetDisabledConfig(kafkaSettings);
        using var client = new ConsumerBuilder<TKey, TValue>(configuration).Build();
        var topic = kafkaSettings.Topic;
        client.Subscribe(topic);

        try
        {
            await ConsumeAsync(client, processor, maxBufferedMessages, maxDegreeOfParallelism, cancellationToken);
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
        Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
        int maxBufferedMessages,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var kafkaProcessor = new ProcessAndOffsetProcessor<TKey, TValue>(
            consumer,
            processor,
            maxBufferedMessages,
            maxDegreeOfParallelism,
            cancellationToken);

        await Task.WhenAll(
            Task.Factory.StartNew(
                () => ConsumeAndProcessAsync(consumer, kafkaProcessor, cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default),
            kafkaProcessor.Completion);
    }

    private static void ConsumeAndProcessAsync<TKey, TValue>(
        IConsumer<TKey, TValue> consumer,
        ProcessAndOffsetProcessor<TKey, TValue> kafkaProcessor,
        CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested is false)
        {
            try
            {
                var kafkaMessage = consumer.Consume(cancellationToken); // Add int and loop
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
