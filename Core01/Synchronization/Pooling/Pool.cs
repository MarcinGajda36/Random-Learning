using System;
using System.Collections.Immutable;
using System.Numerics;

namespace MarcinGajda.Synchronizers.Pooling;

public sealed class PerKeyPool<TKey, TInsance>
    where TKey : notnull
{
    public static PowerOfTwo DefaultSize { get; } = new PowerOfTwo(32);
    private readonly ImmutableArray<TInsance> pool;
    private readonly int poolIndexBitShift;

    public PerKeyPool(Func<TInsance> factory)
        : this(DefaultSize, factory) { }

    public PerKeyPool(PowerOfTwo poolSize, Func<TInsance> factory)
    {
        if (poolSize.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), poolSize, "Pool size has to be bigger then 0.");
        }

        var poolBuilder = ImmutableArray.CreateBuilder<TInsance>((int)poolSize.Value);
        for (int index = 0; index < pool.Length; index++)
        {
            poolBuilder.Add(factory());
        }
        pool = poolBuilder.MoveToImmutable();
        poolIndexBitShift = (sizeof(int) * 8) - BitOperations.TrailingZeroCount(pool.Length);
    }

    public TInsance Get(TKey key)
        => pool[GetIndex(key)];

    private int GetIndex(TKey key)
        => (int)(Hashing.Fibonacci(key) >> poolIndexBitShift);
}
