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
        private readonly IDisposable processingOffsetLink;

        internal Task Completion;

        public ProcessAndOffsetProcessor(
            IConsumer<TKey, TValue> consumer,
            Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
            Settings settings,
            CancellationToken cancellationToken)
        {
            processingBlock = CreateProcessingBlock(processor, settings, cancellationToken);
            var offsetBlock = CreateOffsetBlock(consumer, settings);
            processingOffsetLink = processingBlock.LinkTo(
                offsetBlock,
                new DataflowLinkOptions { PropagateCompletion = true });
            Completion = offsetBlock.Completion;
        }

        public bool Enqueue(ConsumeResult<TKey, TValue> kafkaMessage)
            => processingBlock.Post(kafkaMessage);

        public void Complete()
            => processingBlock.Complete();

        private static TransformBlock<ConsumeResult<TKey, TValue>, ConsumeResult<TKey, TValue>> CreateProcessingBlock(
            Func<ConsumeResult<TKey, TValue>, CancellationToken, ValueTask> processor,
            Settings settings,
            CancellationToken cancellationToken)
        {
            var exceptionHandler = settings.ExceptionHandler;
            return new(
                async result =>
                {
                    try
                    {
                        await processor(result, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        exceptionHandler(KafkaClientOrigin.Processor, ex);
                    }
                    return result;
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = settings.MaxBufferedMessages,
                    MaxDegreeOfParallelism = settings.MaxDegreeOfParallelism,
                    TaskScheduler = settings.ProcessorScheduler,
                    CancellationToken = cancellationToken,
                });
        }

        private static ActionBlock<ConsumeResult<TKey, TValue>> CreateOffsetBlock(
            IConsumer<TKey, TValue> consumer,
            Settings settings)
        {
            var exceptionHandler = settings.ExceptionHandler;
            return new(
                result =>
                {
                    try
                    {
                        consumer.StoreOffset(result);
                    }
                    catch (Exception ex)
                    {
                        exceptionHandler(KafkaClientOrigin.StoreOffset, ex);
                        // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                        if (ex is ConsumeException { Error.IsFatal: true })
                        {
                            throw;
                        }
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = settings.MaxBufferedMessages,
                    TaskScheduler = settings.ConsumerScheduler,
                    SingleProducerConstrained = true,
                });
        }

        public void Dispose()
            => processingOffsetLink.Dispose();
    }
}
