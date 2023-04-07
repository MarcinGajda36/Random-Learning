using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MarcinGajda.Synchronization.Pooling;

namespace MarcinGajda.Synchronization.PerKey;
internal class PerKeyWithPool
{
    private readonly static PowerOfTwo PoolSize = new(32);
    // Index per pool could be calculated once hmm
    // Alternative is 1 shared pool with everything
    private readonly PerKeyPool<Guid, SemaphoreSlim> semaphorePool = new(PoolSize, () => new(1, 1));
    private readonly PerKeyPool<Guid, Queue<int>> queuePool = new(PoolSize, () => new());
    private readonly PerKeyPool<Guid, List<int>> listPool = new(PoolSize, () => new());

    public async Task<int> Caller1(Guid id)
    {
        var semaphore = semaphorePool.Get(id);
        await semaphore.WaitAsync();
        try
        {
            var queue = queuePool.Get(id);
            var result = await SomeRestrictedMethod();
            queue.Enqueue(result);
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<int> Caller2(Guid id)
    {
        var semaphore = semaphorePool.Get(id);
        await semaphore.WaitAsync();
        try
        {
            var list = listPool.Get(id);
            var result = await SomeRestrictedMethod();
            list.Remove(result);
            return result;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private Task<int> SomeRestrictedMethod()
    {
        return Task.FromResult(0);
    }

}
