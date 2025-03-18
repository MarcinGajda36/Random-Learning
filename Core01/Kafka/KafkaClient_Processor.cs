using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Confluent.Kafka;

namespace MarcinGajda.Kafka;
public partial class KafkaClient
{
    private sealed class ProcessAndOffsetProcessor<TKey, TValue> : IDisposable
    {
        private readonly TransformBlock<ConsumeResult<TKey, TValue>, ConsumeResult<TKey, TValue>> processingBlock;
        private readonly ActionBlock<ConsumeResult<TKey, TValue>> offsetBlock;
        private readonly IDisposable processingOffsetLink;

        public Task Completion
            => offsetBlock.Completion;

        public ProcessAndOffsetProcessor(
            IConsumer<TKey, TValue> consumer,
            Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
            int maxBufferedMessages,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken)
        {
            processingBlock = CreateProcessingBlock(processor, maxBufferedMessages, maxDegreeOfParallelism, cancellationToken);
            offsetBlock = CreateOffsetBlock(consumer, maxBufferedMessages);
            processingOffsetLink = processingBlock.LinkTo(
                offsetBlock,
                new DataflowLinkOptions { PropagateCompletion = true });
        }

        public bool Enqueue(ConsumeResult<TKey, TValue> kafkaMessage)
            => processingBlock.Post(kafkaMessage);

        public Task<bool> EnqueueAsync(ConsumeResult<TKey, TValue> kafkaMessage, CancellationToken cancellationToken)
            => processingBlock.SendAsync(kafkaMessage, cancellationToken);

        public void Complete()
            => processingBlock.Complete();

        private static TransformBlock<ConsumeResult<TKey, TValue>, ConsumeResult<TKey, TValue>> CreateProcessingBlock(
            Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
            int maxBufferedMessages,
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
                    BoundedCapacity = maxBufferedMessages,
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    CancellationToken = cancellationToken,
                });

        private static ActionBlock<ConsumeResult<TKey, TValue>> CreateOffsetBlock(IConsumer<TKey, TValue> consumer, int maxBufferedMessages)
            => new(
                result =>
                {
                    try
                    {
                        consumer.StoreOffset(result);
                    }
                    catch (KafkaException) { /*maybe log*/ }
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = maxBufferedMessages,
                    SingleProducerConstrained = true
                });

        public void Dispose()
            => processingOffsetLink.Dispose();
    }
}
