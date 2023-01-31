using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers;

public sealed class Synchronizer : IDisposable
{
    public sealed class Lease : IDisposable
    {
        private SemaphoreSlim? _semaphore;

        public Lease(SemaphoreSlim semaphore)
            => _semaphore = semaphore;

        public void Dispose()
            => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }

    private readonly SemaphoreSlim _semaphoreSlim;

    public Synchronizer(int max = 1)
        => _semaphoreSlim = new SemaphoreSlim(max, max);

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
        using var synchronizer = new Synchronizer();
        using var holder1 = synchronizer.Acquire(CancellationToken.None);
        using var cancel = new CancellationTokenSource();
        using var holder1_1 = synchronizer.Acquire(cancel.Token);
        using var holder2 = await synchronizer.AcquireAsync(CancellationToken.None);
    }
}
