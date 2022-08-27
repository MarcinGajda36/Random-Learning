using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers;

internal sealed class PerKeyConcurrentExclusiveScheduler<TKey>
    where TKey : notnull
{
    private readonly ConcurrentExclusiveSchedulerPair[] pool;

    public PerKeyConcurrentExclusiveScheduler(int? poolSize = null)
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

    public Task<TResult> Schedule<TResult>(
        TKey key,
        OperationType operationType,
        Func<object?, TResult> operation,
        object? argument = null,
        TaskCreationOptions taskCreationOptions = TaskCreationOptions.None,
        CancellationToken cancellationToken = default)
    {
        long index = (uint)key.GetHashCode() % pool.Length;
        var concurrentExclusive = pool[index];
        var scheduler = GetScheduler(operationType, concurrentExclusive);
        return Task.Factory.StartNew(
            operation,
            argument,
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