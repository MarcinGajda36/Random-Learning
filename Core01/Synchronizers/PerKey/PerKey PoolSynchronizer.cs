using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers;

public sealed partial class PoolPerKeySynchronizer<TKey>
    : IDisposable
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
            pool[index] = new SemaphoreSlim(1, 1);
        }
    }

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        var semaphore = pool[GetIndex(key)];
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await resultFactory(key, argument, cancellationToken);
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

    private long GetIndex(TKey key)
        => (uint)key.GetHashCode() % pool.Length;

    private uint GetIndex2(TKey key)
    {
        // This gives better index distribution but needs pool size to be power of 2 to work.
        // https://www.youtube.com/watch?v=9XNcbN08Zvc&list=PLqWncHdBPoD4-d_VSZ0MB0IBKQY0rwYLd&index=5
        var hash = EqualityComparer<TKey>.Default.GetHashCode(key);
        var fibonachi = (uint)hash * 2654435769u;
        var indexBitShift = 32 - BitOperations.TrailingZeroCount(pool.Length);
        return fibonachi >> indexBitShift;
    }

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
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}