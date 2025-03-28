﻿namespace MarcinGajda.Synchronization.Synchronizers;

using System;
using System.Threading;
using System.Threading.Tasks;

public readonly struct AsyncLock(SemaphoreSlim semaphoreSlim) : IDisposable
{
    public AsyncLock(int initialCount, int maxCount)
        : this(new SemaphoreSlim(initialCount, maxCount)) { }

    public AsyncLock(int initialCount)
        : this(initialCount, initialCount) { }

    public AsyncLock()
        : this(1) { }

    public sealed class Releaser(SemaphoreSlim toRelease) : IDisposable
    {
        private SemaphoreSlim? _semaphore = toRelease;

        public void Dispose()
            => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }

    private readonly SemaphoreSlim _semaphoreSlim = semaphoreSlim;

    public Releaser Acquire(CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphoreSlim;
        semaphore.Wait(cancellationToken);
        return new Releaser(semaphore);
    }

    public ValueTask<Releaser> AcquireAsync(CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphoreSlim;
        var wait = semaphore.WaitAsync(cancellationToken);
        return wait.IsCompletedSuccessfully
            ? ValueTask.FromResult(new Releaser(semaphore))
            : CoreAcquireAsync(wait, semaphore);

        static async ValueTask<Releaser> CoreAcquireAsync(Task wait, SemaphoreSlim semaphore)
        {
            await wait;
            return new Releaser(semaphore);
        }
    }

    public TResult Execute<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> function,
        CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphoreSlim;
        semaphore.Wait(cancellationToken);
        try
        {
            return function(argument, cancellationToken);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public TResult Execute<TResult>(
        Func<CancellationToken, TResult> function,
        CancellationToken cancellationToken = default)
        => Execute(
            function,
            static (func, token) => func(token),
            cancellationToken);

    public async ValueTask<TResult> ExecuteAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> function,
        CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphoreSlim;
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await function(argument, cancellationToken);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public ValueTask<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> function,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(
            function,
            static (func, token) => func(token),
            cancellationToken);

    public void Dispose()
        => _semaphoreSlim.Dispose();
}

public static class SynchronizerTests
{
    public static async Task TestAsync()
    {
        using var synchronizer = new AsyncLock();
        var result1 = synchronizer.ExecuteAsync(token => ValueTask.FromResult(1 + 1));
        using var holder1 = await synchronizer.AcquireAsync(CancellationToken.None);
        var result2 = 1 + 1;
    }
}
