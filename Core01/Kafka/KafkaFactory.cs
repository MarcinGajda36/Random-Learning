using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

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
            int.MaxValue,
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

    public static IObservable<ConsumeResult<TKey, TValue>> IObservableTestClient<TKey, TValue>(KafkaSettings kafkaSettings)
        => Observable.Using(
            () => CreateConsumer<TKey, TValue>(kafkaSettings),
            consumer => Observable.Create<ConsumeResult<TKey, TValue>>((observer, cancellation) =>
            {
                string topic = kafkaSettings.Topic;
                consumer.Subscribe(topic);
                Consume(consumer, observer, cancellation);
                return Task.CompletedTask;
            }));

    private static IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(KafkaSettings kafkaSettings)
    {
        var configuration = new ConsumerConfig()
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            GroupId = kafkaSettings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
        };
        return new ConsumerBuilder<TKey, TValue>(configuration).Build();
    }

    private static void Consume<TKey, TValue>(IConsumer<TKey, TValue> consumer, IObserver<ConsumeResult<TKey, TValue>> observer, CancellationToken cancellation)
    {
        try
        {
            while (cancellation.IsCancellationRequested is false)
            {
                observer.OnNext(consumer.Consume(cancellation));
            }
            observer.OnCompleted();
        }
        catch (Exception exception)
        {
            observer.OnError(exception);
        }
        finally
        {
            consumer.Close();
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
                var kafkaMessage = consumer.Consume(cancellationToken);
                kafkaProcessor.Enqueue(kafkaMessage);
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
