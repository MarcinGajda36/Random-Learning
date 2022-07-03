using Microsoft.Extensions.Hosting;
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.RXTests
{
    public class BackgroundStuff : BackgroundService
    {
        private readonly MyQueue myQueue;

        public BackgroundStuff(MyQueue myQueue)
            => this.myQueue = myQueue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            => await myQueue
            .Observable
            .Do(item => Console.WriteLine(item))
            .ToTask(stoppingToken);
    }
}
