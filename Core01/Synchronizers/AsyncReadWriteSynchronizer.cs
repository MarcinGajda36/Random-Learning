using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Synchronizers;

internal class AsyncReadWriteSynchronizer
{
    private class Operation
    {
        private readonly TaskCompletionSource<object> completionSource = new();
        private readonly Func<Task<object>> func;

        public Task<object> Result => completionSource.Task;

        public Operation(Func<Task<object>> func) => this.func = func;

        public async Task Execute()
        {
            try
            {
                completionSource.SetResult(await func());
            }
            catch (Exception exception)
            {
                completionSource.SetException(exception);
            }
        }
    }

    private readonly ConcurrentExclusiveSchedulerPair concurrentExclusive;
    private readonly ActionBlock<Operation> writes;
    private readonly ActionBlock<Operation> reads;

    public AsyncReadWriteSynchronizer()
    {
        concurrentExclusive = new();
        writes = new ActionBlock<Operation>(
            operation => operation.Execute(),
            new ExecutionDataflowBlockOptions
            {
                TaskScheduler = concurrentExclusive.ExclusiveScheduler
            });
        reads = new ActionBlock<Operation>(
            operation => operation.Execute(),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                TaskScheduler = concurrentExclusive.ConcurrentScheduler
            });
    }

    public async Task<T> Write<T>(Func<Task<T>> write)
    {
        var operation = new Operation(async () => await write());
        if (writes.Post(operation))
        {
            return (T)await operation.Result;
        }
        else
        {
            throw null;
        }
    }

    public async Task<T> Read<T>(Func<Task<T>> read)
    {
        var operation = new Operation(async () => await read());
        if (reads.Post(operation))
        {
            return (T)await operation.Result;
        }
        else
        {
            throw null;
        }
    }
}
