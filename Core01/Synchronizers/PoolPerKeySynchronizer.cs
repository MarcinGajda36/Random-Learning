using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers;

public sealed class PoolPerKeySynchronizer<TKey> : IDisposable
    where TKey : notnull
{
    private readonly SemaphoreSlim[] pool;
    private bool disposedValue;

    public PoolPerKeySynchronizer(int? poolSize = null)
    {
        if (poolSize.HasValue && poolSize.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), poolSize, "Pool size has to be bigger then 0.");
        }

        pool = new SemaphoreSlim[poolSize ?? Environment.ProcessorCount];
        for (int index = 0; index < pool.Length; index++)
        {
            pool[index] = new SemaphoreSlim(1);
        }
    }

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        long index = (uint)key.GetHashCode() % pool.Length;
        var semaphore = pool[index];
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await resultFactory(key, argument, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public Task<TResult> SynchronizeAsync<TResult>(
        TKey key,
        Func<TKey, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            resultFactory,
            static (key, factory, cancellation) => factory(key, cancellation),
            cancellationToken);

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Array.ForEach(pool, static semaphore => semaphore.Dispose());
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
