﻿using Confluent.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Kafka;

public record KafkaSettings(string Topic, string BootstrapServers, string GroupId);

public sealed class KafkaFactory
{
    public Task UnboundedParallelismAtLeastOnceClient<TKey, TValue>(
        KafkaSettings kafkaSettings,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor,
        CancellationToken cancellationToken)
        => AtLeastOnceClient(
            kafkaSettings,
            processor,
            DataflowBlockOptions.Unbounded,
            cancellationToken);

    public async Task AtLeastOnceClient<TKey, TValue>(
        KafkaSettings kafkaSettings,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken)
    {
        var configuration = AutoOffsetDisabledConfig(kafkaSettings);
        using var client = new ConsumerBuilder<TKey, TValue>(configuration).Build();
        var topic = kafkaSettings.Topic;
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
        var processingBlock = CreateProcessingBlock(processor, maxDegreeOfParallelism, cancellationToken);
        var offsetBlock = CreateOffsetBlock(consumer, cancellationToken);
        using var link = processingBlock.LinkTo(offsetBlock, new DataflowLinkOptions { PropagateCompletion = true });

        await Task.WhenAll(
            ConsumeAndProcessAsync(consumer, processingBlock, cancellationToken),
            processingBlock.Completion,
            offsetBlock.Completion);
    }

    private static async Task ConsumeAndProcessAsync<TKey, TValue>(
        IConsumer<TKey, TValue> consumer,
        ITargetBlock<ConsumeResult<TKey, TValue>> processorBlock,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var kafkaMessage = consumer.Consume(cancellationToken);
                if (!await processorBlock.SendAsync(kafkaMessage, cancellationToken))
                {
                    await processorBlock.Completion;
                }
            }
            catch (ConsumeException ex)
            {
                // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                if (ex.Error.IsFatal)
                {
                    processorBlock.Complete();
                    throw;
                }
            }
            catch
            {
                processorBlock.Complete();
                throw;
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

    private static ActionBlock<ConsumeResult<TKey, TValue>> CreateOffsetBlock<TKey, TValue>(
        IConsumer<TKey, TValue> consumer,
        CancellationToken cancellationToken)
        => new(
            kafkaMessage =>
            {
                try
                {
                    consumer.StoreOffset(kafkaMessage);
                }
                catch (KafkaException) { }
            },
            new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken });
}