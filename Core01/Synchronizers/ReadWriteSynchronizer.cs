using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Synchronizers;

internal class ReadWriteSynchronizer
{
    private sealed class Operation : IAsyncDisposable
    {
        private readonly TaskCompletionSource<object> completionSource = new();
        private readonly Func<CancellationToken, Task<object>> operation;
        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenRegistration taskCancellation;

        public Task<object> Result => completionSource.Task;

        public Operation(Func<CancellationToken, Task<object>> operation, CancellationToken cancellationToken)
        {
            this.operation = operation;
            this.cancellationToken = cancellationToken;
            taskCancellation = cancellationToken.Register(
                static @this =>
                {
                    var operation = (Operation)@this!;
                    _ = operation.completionSource.TrySetCanceled(operation.cancellationToken);
                },
                this);
        }

        public async Task Run()
        {
            try
            {
                _ = cancellationToken.IsCancellationRequested
                    ? completionSource.TrySetCanceled(cancellationToken)
                    : completionSource.TrySetResult(await operation(cancellationToken));
            }
            catch (Exception exception)
            {
                _ = completionSource.TrySetException(exception);
            }
        }

        public ValueTask DisposeAsync() => taskCancellation.DisposeAsync();
    }

    private readonly ActionBlock<Operation> writes;
    private readonly ActionBlock<Operation> reads;

    public ReadWriteSynchronizer()
    {
        var concurrentExclusive = new ConcurrentExclusiveSchedulerPair();
        writes = new ActionBlock<Operation>(
            static operation => operation.Run(),
            new ExecutionDataflowBlockOptions
            {
                TaskScheduler = concurrentExclusive.ExclusiveScheduler
            });

        reads = new ActionBlock<Operation>(
            static operation => operation.Run(),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                TaskScheduler = concurrentExclusive.ConcurrentScheduler
            });
    }

    public Task<TResult> Write<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        => RunOperation(writes, operation, cancellationToken);

    public Task<TResult> Read<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        => RunOperation(reads, operation, cancellationToken);

    private static async Task<TResult> RunOperation<TResult>(
        ActionBlock<Operation> runner,
        Func<CancellationToken, Task<TResult>> toRun,
        CancellationToken cancellationToken)
    {
        await using var operation = new Operation(
            async (cancellationToken) => (await toRun(cancellationToken))!,
            cancellationToken);

        _ = runner.Post(operation);
        return (TResult)await operation.Result;
    }
}