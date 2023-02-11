using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.Synchronizers;

internal class ConcurrentExclusiveSynchronizer
{
    private interface IOperation
    {
        Task ExecuteAsync();
    }

    private sealed class Operation<TResult> : IOperation, IAsyncDisposable
    {
        private readonly TaskCompletionSource<TResult> completionSource = new();
        private readonly Func<CancellationToken, Task<TResult>> operation;
        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenRegistration taskCancellation;

        public Task<TResult> Task => completionSource.Task;

        public Operation(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            this.operation = operation;
            this.cancellationToken = cancellationToken;
            taskCancellation = cancellationToken.Register(
                static @this =>
                {
                    var operation = (Operation<TResult>)@this!;
                    _ = operation.completionSource.TrySetCanceled(operation.cancellationToken);
                },
                this);
        }

        public async Task ExecuteAsync()
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

    private readonly ActionBlock<IOperation> exclusive;
    private readonly ActionBlock<IOperation> concurrent;

    public ConcurrentExclusiveSynchronizer()
    {
        var concurrentExclusive = new ConcurrentExclusiveSchedulerPair();
        exclusive = new ActionBlock<IOperation>(
            static operation => operation.ExecuteAsync(),
            new ExecutionDataflowBlockOptions
            {
                TaskScheduler = concurrentExclusive.ExclusiveScheduler
            });

        concurrent = new ActionBlock<IOperation>(
            static operation => operation.ExecuteAsync(),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                TaskScheduler = concurrentExclusive.ConcurrentScheduler
            });
    }

    public Task<TResult> ExclusiveAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        => ExecuteOperationAsync(exclusive, operation, cancellationToken);

    public Task<TResult> ConcurrentAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        => ExecuteOperationAsync(concurrent, operation, cancellationToken);

    private static async Task<TResult> ExecuteOperationAsync<TResult>(
        ActionBlock<IOperation> runner,
        Func<CancellationToken, Task<TResult>> toRun,
        CancellationToken cancellationToken)
    {
        await using var operation = new Operation<TResult>(toRun, cancellationToken);
        _ = runner.Post(operation);
        return await operation.Task;
    }
}