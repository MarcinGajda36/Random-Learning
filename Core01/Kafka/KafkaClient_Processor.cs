namespace MarcinGajda.Kafka;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Confluent.Kafka;

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
            Settings settings,
            CancellationToken cancellationToken)
        {
            processingBlock = CreateProcessingBlock(processor, settings, cancellationToken);
            offsetBlock = CreateOffsetBlock(consumer, settings);
            processingOffsetLink = processingBlock.LinkTo(
                offsetBlock,
                new DataflowLinkOptions { PropagateCompletion = true });
        }

        public bool Enqueue(ConsumeResult<TKey, TValue> kafkaMessage)
            => processingBlock.Post(kafkaMessage);

        public void Complete()
            => processingBlock.Complete();

        private static TransformBlock<ConsumeResult<TKey, TValue>, ConsumeResult<TKey, TValue>> CreateProcessingBlock(
            Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
            Settings settings,
            CancellationToken cancellationToken)
            => new(
                async result =>
                {
                    await processor(result, cancellationToken);
                    return result;
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = settings.MaxBufferedMessages,
                    MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism,
                    TaskScheduler = settings.ProcessorScheduler,
                    CancellationToken = cancellationToken,
                });

        private static ActionBlock<ConsumeResult<TKey, TValue>> CreateOffsetBlock(
            IConsumer<TKey, TValue> consumer,
            Settings settings)
            => new(
                result =>
                {
                    try
                    {
                        consumer.StoreOffset(result);
                    }
                    catch (KafkaException exception)
                    {
                        // Maybe log.
                        if (exception.Error.IsFatal)
                        {
                            throw;
                        }
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = settings.MaxBufferedMessages,
                    TaskScheduler = settings.ProcessorScheduler,
                    SingleProducerConstrained = true,
                });

        public void Dispose()
            => processingOffsetLink.Dispose();
    }
}
