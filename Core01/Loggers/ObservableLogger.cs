using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Loggers
{
    public class ObservableLogger : IDisposable
    {
        private readonly BufferBlock<string> sourceBlock = new BufferBlock<string>();
        private readonly IDisposable sub;

        public ObservableLogger()
            => sub = sourceBlock
            .AsObservable()
            .Window(TimeSpan.FromSeconds(5), 127)
            .Select(LogSomwhere)
            .Concat()//Sequencial
            .Subscribe();

        public void Log(string toLog) => sourceBlock.Post(toLog);
        private async Task<Unit> LogSomwhere(IObservable<string> toLog)
        {
            var batch = await toLog.ToList();
            //Log batch
            return Unit.Default;
        }

        public void Dispose() => sub.Dispose();
    }
}
