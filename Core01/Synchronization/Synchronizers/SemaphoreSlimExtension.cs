namespace MarcinGajda.Synchronization.Synchronizers;

using System;
using System.Threading;
using System.Threading.Tasks;

public static class SemaphoreSlimExtension
{
    public sealed class Releaser(SemaphoreSlim toRelease) : IDisposable
    {
        private SemaphoreSlim? _semaphore = toRelease;

        public void Dispose()
            => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }

    public static Releaser Acquire(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken = default)
    {
        semaphoreSlim.Wait(cancellationToken);
        return new Releaser(semaphoreSlim);
    }

    public static ValueTask<Releaser> AcquireAsync(this SemaphoreSlim semaphoreSlim, CancellationToken cancellationToken = default)
    {
        var wait = semaphoreSlim.WaitAsync(cancellationToken);
        return wait.IsCompletedSuccessfully
            ? new(new Releaser(semaphoreSlim))
            : CoreAcquireAsync(wait, semaphoreSlim);

        static async ValueTask<Releaser> CoreAcquireAsync(Task wait, SemaphoreSlim semaphoreSlim)
        {
            await wait;
            return new Releaser(semaphoreSlim);
        }
    }

    public static TResult Execute<TArgument, TResult>(
        this SemaphoreSlim semaphoreSlim,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> function,
        CancellationToken cancellationToken = default)
    {
        semaphoreSlim.Wait(cancellationToken);
        try
        {
            return function(argument, cancellationToken);
        }
        finally
        {
            _ = semaphoreSlim.Release();
        }
    }

    public static TResult Execute<TResult>(
        this SemaphoreSlim semaphoreSlim,
        Func<CancellationToken, TResult> function,
        CancellationToken cancellationToken = default)
        => Execute(
            semaphoreSlim,
            function,
            static (func, token) => func(token),
            cancellationToken);

    public static async ValueTask<TResult> ExecuteAsync<TArgument, TResult>(
        this SemaphoreSlim semaphoreSlim,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> function,
        CancellationToken cancellationToken = default)
    {
        await semaphoreSlim.WaitAsync(cancellationToken);
        try
        {
            return await function(argument, cancellationToken);
        }
        finally
        {
            _ = semaphoreSlim.Release();
        }
    }

    public static ValueTask<TResult> ExecuteAsync<TResult>(
        this SemaphoreSlim semaphoreSlim,
        Func<CancellationToken, ValueTask<TResult>> function,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(
            semaphoreSlim,
            function,
            static (func, token) => func(token),
            cancellationToken);
}

public static class SynchronizerTests
{
    public static async Task TestAsync()
    {
        using var synchronizer = new SemaphoreSlim(1, 1);
        var result1 = synchronizer.ExecuteAsync(token => ValueTask.FromResult(1 + 1));
        using var holder1 = await synchronizer.AcquireAsync(CancellationToken.None);
        var result2 = 1 + 1;
    }
}
