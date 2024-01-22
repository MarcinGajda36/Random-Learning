using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace MarcinGajda.Kafka;
public partial class KafkaFactory
{
    private sealed class ProcessAndOffsetProcessor<TKey, TValue> : IDisposable
    {
        private readonly Subject<ConsumeResult<TKey, TValue>> queue = new();

        public Task Completion { get; }

        public ProcessAndOffsetProcessor(
            IConsumer<TKey, TValue> consumer,
            Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken)
        {
            Completion = queue
                .Select(result => Observable.FromAsync(async token =>
                {
                    await processor(result, token);
                    return result;
                }))
                .Merge(maxDegreeOfParallelism)
                .Select(result =>
                {
                    try
                    {
                        consumer.StoreOffset(result);
                    }
                    catch (KafkaException) { /*maybe log*/ }
                    return Unit.Default;
                })
                .ToTask(cancellationToken);
        }

        public void Enqueue(ConsumeResult<TKey, TValue> kafkaMessage)
            => queue.OnNext(kafkaMessage);

        public void Complete()
            => queue.OnCompleted();

        public void Dispose()
        {
            queue.Dispose();
        }
    }
}
