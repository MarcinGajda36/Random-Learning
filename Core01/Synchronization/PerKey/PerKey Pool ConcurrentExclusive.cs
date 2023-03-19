using System;
using System.Threading;
using System.Threading.Tasks;
using MarcinGajda.Synchronization.Synchronizers;
using MarcinGajda.Synchronizers.Pooling;

namespace MarcinGajda.Synchronizers;

public sealed partial class PoolPerKeyConcurrentExclusive<TKey>
    where TKey : notnull
{
    public static PowerOfTwo DefaultSize { get; } = new PowerOfTwo(32);
    readonly PerKeyPool<TKey, ConcurrentExclusiveSynchronizer> pool;

    public PoolPerKeyConcurrentExclusive()
        : this(DefaultSize) { }

    public PoolPerKeyConcurrentExclusive(PowerOfTwo poolSize)
        => pool = new(DefaultSize, () => new ConcurrentExclusiveSynchronizer());

    public Task<TResult> ConcurrentAsync<TResult>(
        TKey key,
        Func<CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => pool.Get(key).ConcurrentAsync(resultFactory, cancellationToken);

    public Task<TResult> ExclusiveAsync<TResult>(
        TKey key,
        Func<CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => pool.Get(key).ExclusiveAsync(resultFactory, cancellationToken);
}
