using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MarcinGajda.DataflowTests
{
    public class DataflowTests
    {
        public Task<int> TestTaskFactory()
        {
            return Task.Factory.StartNew((x) => 5, 4, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
        }
        public static async Task Test()
        {
            var blockOptions = new DataflowBlockOptions() { };
            var bufferBlock = new BufferBlock<int>();
            var actionBlock = new ActionBlock<int[]>(async i =>
            {
                await Task.Delay(1);
                Console.WriteLine(i);
            });
            var transformBlock = new TransformBlock<int, string>(async i =>
            {
                await Task.Delay(1);
                return i.ToString();
            });
            var batchBlock = new BatchBlock<int>(10);
            IDisposable link = batchBlock.LinkTo(actionBlock);
        }


        private static Task Produce(BufferBlock<int> queue, IEnumerable<int> values)
            => Task.WhenAll(values.Select(queue.SendAsync));
        public static Task ProduceAll(BufferBlock<int> queue)
        {
            Task producer1 = Produce(queue, Enumerable.Range(0, 10));
            Task producer2 = Produce(queue, Enumerable.Range(10, 10));
            Task producer3 = Produce(queue, Enumerable.Range(20, 10));
            return Task.WhenAll(producer1, producer2, producer3);
        }

        [TestMethod]
        public static async Task ConsumerReceivesCorrectValues()
        {
            var results = new List<int>();

            // Define the mesh.
            var queue = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 5, });
            var consumerOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1, };
            var consumer = new ActionBlock<int>(results.Add, consumerOptions);
            _ = queue.LinkTo(consumer, new DataflowLinkOptions { PropagateCompletion = true, });

            // Start the producers.
            await ProduceAll(queue);
            queue.Complete();

            // Wait for everything to complete.
            await consumer.Completion;

            // Ensure the consumer got what the producer sent.
            Assert.IsTrue(results.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 30)));
        }

        [TestMethod]
        public async Task ConsumerReceivesCorrectValues1()
        {
            var results1 = new List<int>();
            var results2 = new List<int>();
            var results3 = new List<int>();

            // Define the mesh.
            var queue = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 5, });
            var consumerOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1, };
            var consumer1 = new ActionBlock<int>(results1.Add, consumerOptions);
            var consumer2 = new ActionBlock<int>(results2.Add, consumerOptions);
            var consumer3 = new ActionBlock<int>(results3.Add, consumerOptions);
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true, };
            _ = queue.LinkTo(consumer1, linkOptions);
            _ = queue.LinkTo(consumer2, linkOptions);
            _ = queue.LinkTo(consumer3, linkOptions);

            // Start the producers.
            await ProduceAll(queue);
            queue.Complete();

            // Wait for everything to complete.
            await Task.WhenAll(consumer1.Completion, consumer2.Completion, consumer3.Completion);

            // Ensure the consumer got what the producer sent.
            IEnumerable<int> results = results1.Concat(results2).Concat(results3);
            Assert.IsTrue(results.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 30)));
        }

    }
}
