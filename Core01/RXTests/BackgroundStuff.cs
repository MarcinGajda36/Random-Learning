using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MarcinGajda.RXTests
{
    public class BackgroundStuff : BackgroundService
    {
        private readonly MyQueue myQueue;

        public BackgroundStuff(MyQueue myQueue)
        {
            this.myQueue = myQueue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await myQueue.Observable
                 .Do(item => Console.WriteLine(item))
                 .ToTask(stoppingToken);
        }
    }
}
