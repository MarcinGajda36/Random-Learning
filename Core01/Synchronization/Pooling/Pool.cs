using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Threading;

namespace MarcinGajda.Synchronizers.Pooling;

public class PerKeyPool<TKey, TInsance>
    where TKey : notnull
{
    public static PowerOfTwo DefaultSize { get; } = new PowerOfTwo(32);
    private readonly int poolIndexBitShift;
    protected ImmutableArray<TInsance> Pool { get; }

    public PerKeyPool(Func<TInsance> factory)
        : this(DefaultSize, factory) { }

    public PerKeyPool(PowerOfTwo poolSize, Func<TInsance> factory)
    {
        if (poolSize.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), poolSize, "Pool size has to be bigger then 0.");
        }

        var poolBuilder = ImmutableArray.CreateBuilder<TInsance>((int)poolSize.Value);
        for (int index = 0; index < Pool.Length; index++)
        {
            poolBuilder.Add(factory());
        }
        Pool = poolBuilder.MoveToImmutable();
        poolIndexBitShift = (sizeof(int) * 8) - BitOperations.TrailingZeroCount(Pool.Length);
    }

    public TInsance Get(TKey key)
        => Pool[GetIndex(key)];

    private int GetIndex(TKey key)
        => (int)(Hashing.Fibonacci(key) >> poolIndexBitShift);
}

public class PerKeyDisposablePoolV2<TKey, TInsance>
    : PerKeyPool<TKey, TInsance>, IDisposable
    where TInsance : IDisposable
    where TKey : notnull
{
    private bool disposedValue;

    public PerKeyDisposablePoolV2(Func<TInsance> factory) : base(factory) { }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var instance in Pool)
                {
                    instance.Dispose();
                }
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

public class PerKeySynchronizerV3<TKey>
    : PerKeyDisposablePoolV2<TKey, SemaphoreSlim>
    where TKey : notnull
{
    public PerKeySynchronizerV3()
        : base(() => new SemaphoreSlim(1, 1)) { }
}
