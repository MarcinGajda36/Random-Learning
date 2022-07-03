using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Loggers
{
    public class DataflowLogger : IAsyncDisposable
    {
        private readonly BatchBlock<string> logsBuffer;
        private readonly ActionBlock<string[]> logger;
        private readonly Func<Task> intervalCheckerCancelation;
        public DataflowLogger(int batchSize = 10) : this(batchSize, TimeSpan.FromSeconds(3)) { }
        public DataflowLogger(int batchSize, TimeSpan logInterval)
        {
            logger = new ActionBlock<string[]>(async loggs =>
            {
                foreach (string log in loggs)
                {
                    await Task.Delay(1);
                    Console.WriteLine(log);
                }
            });
            logsBuffer = new BatchBlock<string>(batchSize);
            _ = logsBuffer.LinkTo(logger, new DataflowLinkOptions()
            {
                PropagateCompletion = true,
            });

            intervalCheckerCancelation = CreateChecker(logInterval);
        }

        private Func<Task> CreateChecker(TimeSpan logInterval)
        {
            var intervalCancelation = new CancellationTokenSource();
            Task IntervalChecker = CreateIntervalCheckerTask(logInterval, intervalCancelation);
            Func<Task> cancelation = CreateIntervalCheckerCancelation(intervalCancelation, IntervalChecker);
            return cancelation;
        }

        private static Func<Task> CreateIntervalCheckerCancelation(CancellationTokenSource intervalCancelation, Task IntervalChecker)
            => async () =>
            {
                try
                {
                    intervalCancelation.Cancel();
                    await IntervalChecker;
                }
                catch { /*taskCanceledException*/ }
                finally { intervalCancelation.Dispose(); }
            };

        private Task CreateIntervalCheckerTask(TimeSpan logInterval, CancellationTokenSource intervalCancelation) => Task.Factory.StartNew(async () =>
        {
            while (!intervalCancelation.Token.IsCancellationRequested)
            {
                await Task.Delay(logInterval, intervalCancelation.Token);
                logsBuffer.TriggerBatch();
            }
        }, intervalCancelation.Token, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness, TaskScheduler.Default).Unwrap();

        public async void Log(string message)
            => await logsBuffer.SendAsync(message);

        private bool disposed = false;
        public async ValueTask DisposeAsync()
        {
            if (disposed)
            {
                return;
            }
            else
            {
                await Disposing();
                disposed = true;
            }
        }

        private Task Disposing()
        {
            Task cancellation = intervalCheckerCancelation();
            Task completition = CompleteBlocks();
            return Task.WhenAll(cancellation, completition);
        }

        private Task CompleteBlocks()
        {
            logsBuffer.TriggerBatch();
            logsBuffer.Complete();
            return logger.Completion;
        }

        ~DataflowLogger()
        {
            _ = intervalCheckerCancelation();
            logsBuffer.TriggerBatch();
            logsBuffer.Complete();
        }
    }
    public static class DataflowLoggerTests
    {
        public static async Task Test1Async()
        {
            await using var logger = new DataflowLogger(10);
            logger.Log("1");
            logger.Log("2");
            await Task.Delay(TimeSpan.FromSeconds(4));
            logger.Log("3");
        }
    }
}
