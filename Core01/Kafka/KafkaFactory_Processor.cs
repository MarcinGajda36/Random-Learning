using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Confluent.Kafka;

namespace MarcinGajda.Kafka;
public partial class KafkaFactory
{
    private sealed class ProcessAndOffsetProcessor<TKey, TValue> : IDisposable
    {
        private readonly record struct ThisResultPair(
            ProcessAndOffsetProcessor<TKey, TValue> This,
            ConsumeResult<TKey, TValue> Result);

        private readonly IConsumer<TKey, TValue> consumer;
        private readonly Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor;
        private readonly CancellationToken cancellationToken;
        private readonly TransformBlock<ThisResultPair, ThisResultPair> processingBlock;
        private readonly ActionBlock<ThisResultPair> offsetBlock;
        private readonly IDisposable processingOffsetLink;

        public Task Completion => offsetBlock.Completion;

        public ProcessAndOffsetProcessor(
            IConsumer<TKey, TValue> consumer,
            Func<ConsumeResult<TKey, TValue>, CancellationToken, Task> processor,
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken)
        {
            this.consumer = consumer;
            this.processor = processor;
            this.cancellationToken = cancellationToken;
            processingBlock = CreateProcessingBlock(maxDegreeOfParallelism, cancellationToken);
            offsetBlock = CreateOffsetBlock();
            processingOffsetLink = processingBlock.LinkTo(
                offsetBlock,
                new DataflowLinkOptions { PropagateCompletion = true });
        }

        public bool Enqueue(ConsumeResult<TKey, TValue> kafkaMessage)
            => processingBlock.Post(new(this, kafkaMessage));

        public void Complete()
            => processingBlock.Complete();

        private static TransformBlock<ThisResultPair, ThisResultPair> CreateProcessingBlock(
            int maxDegreeOfParallelism,
            CancellationToken cancellationToken)
            => new(
                static async thisResultPair =>
                {
                    var (@this, result) = thisResultPair;
                    await @this.processor(result, @this.cancellationToken);
                    return thisResultPair;
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = maxDegreeOfParallelism,
                    CancellationToken = cancellationToken,
                });

        private static ActionBlock<ThisResultPair> CreateOffsetBlock()
            => new(
                static thisResultPair =>
                {
                    try
                    {
                        var (@this, result) = thisResultPair;
                        @this.consumer.StoreOffset(result);
                    }
                    catch (KafkaException) { /*maybe log*/ }
                },
                new ExecutionDataflowBlockOptions { SingleProducerConstrained = true });

        public void Dispose()
            => processingOffsetLink.Dispose();
    }
}
