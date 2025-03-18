using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Confluent.Kafka;

namespace MarcinGajda.Kafka;
public partial class KafkaFactory
{
    private sealed class ProcessAndOffsetProcessorV2<TKey, TValue> : IDisposable
    {
        private readonly IConsumer<TKey, TValue> consumer;
        private readonly Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor;
        private readonly TransformBlock<ConsumeResult<TKey, TValue>, ConsumeResult<TKey, TValue>> processingBlock;
        private readonly ActionBlock<ConsumeResult<TKey, TValue>> offsetBlock;
        private readonly IDisposable processingOffsetLink;

        public Task Completion => offsetBlock.Completion;

        public ProcessAndOffsetProcessorV2(
            IConsumer<TKey, TValue> consumer,
            Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken)
        {
            this.consumer = consumer;
            this.processor = processor;
            processingBlock = CreateProcessingBlock(maxDegreeOfParallelism, cancellationToken);
            offsetBlock = CreateOffsetBlock();
            processingOffsetLink = processingBlock.LinkTo(
                offsetBlock,
                new DataflowLinkOptions { PropagateCompletion = true });
        }

        public bool Enqueue(ConsumeResult<TKey, TValue> kafkaMessage)
            => processingBlock.Post(kafkaMessage);

        public void Complete()
            => processingBlock.Complete();

        private TransformBlock<ConsumeResult<TKey, TValue>, ConsumeResult<TKey, TValue>> CreateProcessingBlock(
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken)
            => new(
                async result =>
                {
                    await processor(result, cancellationToken);
                    return result;
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    CancellationToken = cancellationToken,
                });

        private ActionBlock<ConsumeResult<TKey, TValue>> CreateOffsetBlock()
            => new(
                result =>
                {
                    try
                    {
                        consumer.StoreOffset(result);
                    }
                    catch (KafkaException) { /*maybe log*/ }
                },
                new ExecutionDataflowBlockOptions { SingleProducerConstrained = true });

        public void Dispose()
            => processingOffsetLink.Dispose();
    }

    private sealed class ProcessAndOffsetProcessorV1<TKey, TValue> : IDisposable
    {
        private readonly Subject<ConsumeResult<TKey, TValue>> queue = new();

        public Task Completion { get; }

        public ProcessAndOffsetProcessorV1(
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
                .Merge(maxDegreeOfParallelism) // WRONG. This changes order, making StoreOffset(...) go back and forth in time.
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
