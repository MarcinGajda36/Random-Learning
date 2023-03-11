using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MarcinGajda.channels
{
    public class BackgroundStuff : BackgroundService
    {
        private readonly ProducerConsumer producerConsumer;

        public BackgroundStuff(ProducerConsumer producerConsumer)
            => this.producerConsumer = producerConsumer;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var reader = producerConsumer.Reader;
            while (stoppingToken.IsCancellationRequested
                && await reader.WaitToReadAsync(stoppingToken)
                && reader.TryRead(out var read))
            {
                Console.WriteLine(read);
            }
        }
    }
}
