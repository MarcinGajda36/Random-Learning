namespace MarcinGajda.Kafka;

using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

public sealed partial class KafkaClient
{
    public sealed record Settings(
        string Topic,
        string BootstrapServers,
        string GroupId)
    {
        public int MaxDegreeOfParallelism { get; init; } = 1;
        public int MaxBufferedMessages { get; init; } = 4096;
        public int ConsumeMillisecondsTimeout { get; init; } = 1024;
        public TaskScheduler ConsumerScheduler { get; init; } = TaskScheduler.Default;
        public TaskScheduler ProcessorScheduler { get; init; } = TaskScheduler.Default;
    }

    public static async Task AtLeastOnceClient<TKey, TValue>(
        Settings settings,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
        CancellationToken cancellationToken = default)
    {
        var configuration = AutoOffsetDisabledConfig(settings);
        using var client = new ConsumerBuilder<TKey, TValue>(configuration).Build();
        await ConsumeAsync(client, processor, settings, cancellationToken);
    }

    private static ConsumerConfig AutoOffsetDisabledConfig(Settings kafkaSettings)
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
        Settings settings,
        CancellationToken cancellationToken)
    {
        using var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationToken = cancellationSource.Token;
        using var kafkaProcessor = new ProcessAndOffsetProcessor<TKey, TValue>(
            consumer,
            processor,
            settings,
            cancellationToken);

        var processorTask = kafkaProcessor.Completion;
        var consumerTask = Task.Factory.StartNew(
            () =>
            {
                consumer.Subscribe(settings.Topic);
                try
                {
                    ConsumeAndProcess(consumer, kafkaProcessor, settings.ConsumeMillisecondsTimeout, cancellationToken);
                }
                finally
                {
                    consumer.Close();
                }
            },
            cancellationToken,
            TaskCreationOptions.LongRunning,
            settings.ConsumerScheduler);

        var firstToFinish = await Task.WhenAny(consumerTask, processorTask);
        await cancellationSource.CancelAsync();
        await Task.WhenAll(firstToFinish, consumerTask, processorTask);
    }

    private static void ConsumeAndProcess<TKey, TValue>(
        IConsumer<TKey, TValue> consumer,
        ProcessAndOffsetProcessor<TKey, TValue> kafkaProcessor,
        int consumeMillisecondsTimeout,
        CancellationToken cancellationToken)
    {
        while (cancellationToken.IsCancellationRequested is false)
        {
            try
            {
                var kafkaMessage = consumer.Consume(consumeMillisecondsTimeout);
                if (kafkaMessage != null)
                {
                    if (kafkaProcessor.Enqueue(kafkaMessage) is false)
                    {
                        return;
                    }
                }
            }
            catch (ConsumeException ex)
            {
                // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                // Maybe log.
                if (ex.Error.IsFatal)
                {
                    kafkaProcessor.Complete();
                    throw;
                }
            }
            catch
            {
                // Maybe log x2.
                kafkaProcessor.Complete();
                throw;
            }
        }
    }
}
