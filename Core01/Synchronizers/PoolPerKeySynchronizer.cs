using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers;

public sealed class PoolPerKeySynchronizer<TKey>
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
        long index = GetIndex(key);
        var semaphore = pool[index];
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

    public async Task<TResult> SynchronizeAllAsync<TAgument, TResult>(
        TAgument argument,
        Func<TAgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        static void ReleaseAll(SemaphoreSlim[] pool, int index)
        {
            for (int toRelease = index; toRelease >= 0; toRelease--)
            {
                _ = pool[toRelease].Release();
            }
        }

        int index = 0;
        do
        {
            try
            {
                await pool[index].WaitAsync(cancellationToken);
                index += 1;
            }
            catch
            {
                ReleaseAll(pool, index);
                throw;
            }
        } while (index < pool.Length - 1);

        try
        {
            return await resultFactory(argument, cancellationToken);
        }
        finally
        {
            ReleaseAll(pool, index);
        }
    }

    public async Task<TResult> SynchronizeManyAsync<TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        static void ReleaseLocked(SemaphoreSlim[] pool, Stack<long> locked)
        {
            while (locked.TryPop(out long toRelease))
            {
                _ = pool[toRelease].Release();
            }
        }

        var indexes = new SortedSet<long>();
        foreach (var key in keys)
        {
            long index = GetIndex(key);
            _ = indexes.Add(index);
        }

        var locked = new Stack<long>(indexes.Count);
        foreach (long toLock in indexes)
        {
            try
            {
                await pool[toLock].WaitAsync(cancellationToken);
                locked.Push(toLock);
            }
            catch
            {
                ReleaseLocked(pool, locked);
                throw;
            }
        }

        try
        {
            return await resultFactory(argument, cancellationToken);
        }
        finally
        {
            ReleaseLocked(pool, locked);
        }
    }

    private long GetIndex(TKey key)
        => (uint)key.GetHashCode() % pool.Length;

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