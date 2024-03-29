﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers;

public partial class PoolPerKeySynchronizer<TKey>
{
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

        for (int index = 0; index < pool.Length; index++)
        {
            try
            {
                await pool[index].WaitAsync(cancellationToken);
            }
            catch
            {
                ReleaseAll(pool, index - 1);
                throw;
            }
        }

        try
        {
            return await resultFactory(argument, cancellationToken);
        }
        finally
        {
            ReleaseAll(pool, pool.Length - 1);
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
            _ = indexes.Add(GetIndex(key));
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
}
