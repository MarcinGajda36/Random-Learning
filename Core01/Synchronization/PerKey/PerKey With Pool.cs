using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarcinGajda.Synchronizers;
using MarcinGajda.Synchronizers.Pooling;

namespace MarcinGajda.Synchronization.PerKey;
internal class PerKeyWithPool
{
    private readonly static PowerOfTwo PoolSize = new(32);
    private readonly PoolPerKeySynchronizerPerf<Guid> synchronizer = new(PoolSize);
    private readonly PerKeyPool<Guid, Queue<int>> queuePool = new(PoolSize, () => new());


    public Task<int> Caller1(Guid id)
    {
        var queue = queuePool.Get(id);
        return synchronizer.SynchronizeAsync(id, queue, async (queue, cancellationToken) =>
        {
            var result = await SomeRestrictedMethod();
            queue.Enqueue(result);
            return result;
        });
    }

    private Task<int> SomeRestrictedMethod()
    {
        return Task.FromResult(0);
    }


}
