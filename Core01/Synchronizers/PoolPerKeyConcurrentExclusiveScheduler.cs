using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers;

internal sealed class PoolPerKeyConcurrentExclusiveScheduler<TKey>
    where TKey : notnull
{
    private readonly ConcurrentExclusiveSchedulerPair[] pool;

    public PoolPerKeyConcurrentExclusiveScheduler(int? poolSize = null)
    {
        if (poolSize.HasValue && poolSize.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), poolSize, "Pool size has to be bigger then 0.");
        }

        pool = new ConcurrentExclusiveSchedulerPair[poolSize ?? Environment.ProcessorCount];
        for (int index = 0; index < pool.Length; index++)
        {
            pool[index] = new ConcurrentExclusiveSchedulerPair();
        }
    }

    public Task<TResult> Schedule<TArgument, TResult>(
        TKey key,
        TArgument argument,
        OperationType operationType,
        Func<TKey, TArgument, CancellationToken, TResult> resultFactory,
        TaskCreationOptions taskCreationOptions = TaskCreationOptions.None,
        CancellationToken cancellationToken = default)
    {
        long index = (uint)key.GetHashCode() % pool.Length;
        var concurrentExclusive = pool[index];
        var scheduler = GetScheduler(operationType, concurrentExclusive);
        return Task.Factory.StartNew(
            () => resultFactory(key, argument, cancellationToken),
            cancellationToken,
            taskCreationOptions,
            scheduler);
    }

    private static TaskScheduler GetScheduler(OperationType operationType, ConcurrentExclusiveSchedulerPair concurrentExclusive)
        => operationType switch
        {
            OperationType.Exclusive => concurrentExclusive.ExclusiveScheduler,
            OperationType.Concurrent => concurrentExclusive.ConcurrentScheduler,
            var unknown => throw new ArgumentOutOfRangeException(nameof(operationType), unknown, "Unknown value."),
        };
}