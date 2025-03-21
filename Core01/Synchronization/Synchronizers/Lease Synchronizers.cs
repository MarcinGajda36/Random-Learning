using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Synchronizers;

public sealed class AsyncLock(int max = 1) : IDisposable
{
    public struct Lease(SemaphoreSlim toRelease) : IDisposable
    {
        private SemaphoreSlim? _semaphore = toRelease;

        public void Dispose()
            => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }

    private readonly SemaphoreSlim _semaphoreSlim = new(max, max);

    public Lease Acquire(CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphoreSlim;
        semaphore.Wait(cancellationToken);
        return new Lease(semaphore);
    }

    public ValueTask<Lease> AcquireAsync(CancellationToken cancellationToken = default)
    {
        var semaphore = _semaphoreSlim;
        var wait = semaphore.WaitAsync(cancellationToken);
        return wait.IsCompletedSuccessfully
            ? ValueTask.FromResult(new Lease(semaphore))
            : CoreAcquireAsync(wait, semaphore);

        static async ValueTask<Lease> CoreAcquireAsync(Task wait, SemaphoreSlim semaphore)
        {
            await wait;
            return new Lease(semaphore);
        }
    }

    public void Dispose()
        => _semaphoreSlim.Dispose();
}

public sealed class Synchronizer1(int max = 1) : IDisposable
{
    public sealed class Lease(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        public void Dispose()
            => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }

    private readonly SemaphoreSlim _semaphoreSlim = new(max, max);

    public Lease Acquire(CancellationToken cancellationToken = default)
    {
        _semaphoreSlim.Wait(cancellationToken);
        return new Lease(_semaphoreSlim);
    }

    public async Task<Lease> AcquireAsync(CancellationToken cancellationToken = default)
    {
        await _semaphoreSlim.WaitAsync(cancellationToken);
        return new Lease(_semaphoreSlim);
    }

    public void Dispose()
        => _semaphoreSlim.Dispose();
}

public static class SynchronizerTests
{
    public static async Task TestAsync()
    {
        using var synchronizer = new Synchronizer1();
        using var holder1 = synchronizer.Acquire(CancellationToken.None);
        using var cancel = new CancellationTokenSource();
        using var holder1_1 = synchronizer.Acquire(cancel.Token);
        using var holder2 = await synchronizer.AcquireAsync(CancellationToken.None);
    }
}
