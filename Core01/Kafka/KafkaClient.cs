namespace MarcinGajda.Kafka;

using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

public enum KafkaClientOrigin
{
    Consumer,
    Processor,
    StoreOffset
}

public sealed partial class KafkaClient
{
    public sealed record Settings(
        string Topic,
        string BootstrapServers,
        string GroupId)
    {
        public int MaxDegreeOfParallelism { get; init; } = 1;
        public int MaxBufferedMessages { get; init; } = 4096;
        public TimeSpan ConsumeTimeout { get; init; } = TimeSpan.FromSeconds(1);
        public TaskScheduler ConsumerScheduler { get; init; } = TaskScheduler.Default;
        public TaskScheduler ProcessorScheduler { get; init; } = TaskScheduler.Default;
        public Action<KafkaClientOrigin, Exception> ExceptionHandler { get; init; } = (_, _) => { };
    }

    public static Task AtLeastOnceAsync<TKey, TValue>(
        Settings settings,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(processor);
        return AtLeastOnceCoreAsync(settings, processor, cancellationToken);
    }

    private static async Task AtLeastOnceCoreAsync<TKey, TValue>(
        Settings settings,
        Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
        CancellationToken cancellationToken)
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
                    ConsumeAndProcess(consumer, kafkaProcessor, settings, cancellationToken);
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
        Settings settings,
        CancellationToken cancellationToken)
    {
        var exceptionHandler = settings.ExceptionHandler;
        var consumeTimeout = settings.ConsumeTimeout;
        while (cancellationToken.IsCancellationRequested is false)
        {
            try
            {
                var kafkaMessage = consumer.Consume(consumeTimeout);
                if (kafkaMessage != null)
                {
                    if (kafkaProcessor.Enqueue(kafkaMessage) is false)
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                exceptionHandler(KafkaClientOrigin.Consumer, ex);
                // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                if (ex is ConsumeException { Error.IsFatal: true })
                {
                    kafkaProcessor.Complete();
                    throw;
                }
            }
        }
    }
}
