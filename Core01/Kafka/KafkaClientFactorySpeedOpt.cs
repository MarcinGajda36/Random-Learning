using Confluent.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Kafka;

public class KafkaClientFactorySpeedOpt
{
    public async Task AtLeastOnceClient<TKey, TValue>(
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
        catch (OperationCanceledException)
        {
            //Log
        }
        catch (Exception)
        {
            //Log
        }

        client.Close();
    }

    private static ConsumerConfig AutoOffsetDisabledConfig(KafkaSettings kafkaSettings)
        => new()
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            GroupId = kafkaSettings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoOffsetStore = false,
        };

    private async Task ConsumeAsync<TKey, TValue>(
        IConsumer<TKey, TValue> consumer,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var processingBlock = CreateProcessingBlock(processor, maxDegreeOfParallelism, cancellationToken);
        var offsetBlock = CreateOffsetBlock(consumer, cancellationToken);
        using var link = processingBlock.LinkTo(offsetBlock);

        await await Task.WhenAny(
            ConsumeAndProcessAsync(consumer, processingBlock, cancellationToken),
            processingBlock.Completion,
            offsetBlock.Completion);
    }

    private async Task ConsumeAndProcessAsync<TKey, TValue>(
        IConsumer<TKey, TValue> consumer,
        ITargetBlock<ConsumeResult<TKey, TValue>> processorBlock,
        CancellationToken cancellationToken)
    {
        var consumerCancellation = Tuple.Create(consumer, cancellationToken);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var kafkaMessage = await Task.Factory.StartNew(
                    static consumerCancellation =>
                    {
                        var (consumer, cancellationToken) = (Tuple<IConsumer<TKey, TValue>, CancellationToken>)consumerCancellation!;
                        return consumer.Consume(cancellationToken);
                    },
                    consumerCancellation,
                    cancellationToken);
                if (!await processorBlock.SendAsync(kafkaMessage, cancellationToken))
                {
                    return;
                }
            }
            catch (ConsumeException e)
            {
                //Log
                if (e.Error.IsFatal)
                {
                    // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                    return;
                }
            }
        }
    }

    private static TransformBlock<ConsumeResult<TKey, TValue>, ConsumeResult<TKey, TValue>> CreateProcessingBlock<TKey, TValue>(
        Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
        => new(
            async kafkaMessage =>
            {
                await processor(kafkaMessage, cancellationToken);
                return kafkaMessage;
            },
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken,
            });

    private ActionBlock<ConsumeResult<TKey, TValue>> CreateOffsetBlock<TKey, TValue>(
        IConsumer<TKey, TValue> consumer,
        CancellationToken cancellationToken)
        => new(
            kafkaMessage =>
            {
                try
                {
                    consumer.StoreOffset(kafkaMessage);
                }
                catch (KafkaException)
                {
                    //Log
                }
            },
            new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken });
}
