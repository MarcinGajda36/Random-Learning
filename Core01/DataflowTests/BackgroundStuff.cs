using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Hosting;

namespace MarcinGajda.DataflowTests
{
    public class BackgroundStuff : BackgroundService
    {
        private readonly MyQueue myQueue;

        public BackgroundStuff(MyQueue myQueue)
            => this.myQueue = myQueue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var actionBlock = new ActionBlock<int>(
                element => Console.WriteLine(element),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                });

            using var cancellation = stoppingToken.Register(actionBlock.Complete);
            using var link = myQueue.SourceBlock.LinkTo(actionBlock);
            await actionBlock.Completion;
        }
    }
}
