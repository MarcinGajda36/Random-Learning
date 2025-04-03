namespace MarcinGajda.Synchronization.Synchronizers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public static class TaskSequencial
{
    private interface IOperation
    {
        Task ExecuteAsync();
    }

    private sealed class Operation<TResult> : IOperation, IAsyncDisposable
    {
        private readonly TaskCompletionSource<TResult> completionSource = new();
        private readonly Func<CancellationToken, ValueTask<TResult>> operation;
        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenRegistration taskCancellation;

        public Task<TResult> Task
            => completionSource.Task;

        public Operation(Func<CancellationToken, ValueTask<TResult>> operation, CancellationToken cancellationToken)
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
            var completionSource_ = completionSource;
            try
            {
                var cancellationToken_ = cancellationToken;
                _ = cancellationToken_.IsCancellationRequested
                    ? completionSource_.TrySetCanceled(cancellationToken_)
                    : completionSource_.TrySetResult(await operation(cancellationToken_));
            }
            catch (Exception exception)
            {
                _ = completionSource_.TrySetException(exception);
            }
        }

        public ValueTask DisposeAsync()
            => taskCancellation.DisposeAsync();
    }

    private static readonly ActionBlock<IOperation> globalSequence
        = new(static operation => operation.ExecuteAsync(), new() { BoundedCapacity = 4096 });

    public static async Task<TResult> AddNextGlobal<TResult>(
        Func<CancellationToken, ValueTask<TResult>> function,
        CancellationToken cancellationToken)
    {
        await using var operation = new Operation<TResult>(function, cancellationToken);
        _ = await globalSequence.SendAsync(operation);
        return await operation.Task;
    }

    public static async Task<IReadOnlyList<TResult>> SelectWhenAllSequencial<TSource, TResult>(
        this IEnumerable<TSource> sources,
        Func<TSource, CancellationToken, ValueTask<TResult>> function,
        CancellationToken cancellationToken)
    {
        var results = sources.TryGetNonEnumeratedCount(out var count)
            ? new List<TResult>(count)
            : new List<TResult>();

        foreach (var source in sources)
        {
            results.Add(await function(source, cancellationToken));
        }

        return results;
    }

    public static Task<TResult> WhenAll<TResult>(
        IEnumerable<Func<CancellationToken, Task<TResult>>> funcs,
        CancellationToken cancellationToken)
    {

        return null;
    }
}
