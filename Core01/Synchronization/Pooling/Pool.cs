using System;
using System.Collections.Immutable;
using System.Numerics;
using System.Threading;
using MarcinGajda.Synchronization.PerKey;

namespace MarcinGajda.Synchronization.Pooling;

// TODO idea: Pool<DataflowBlock> with linking
public class PerKeyPool<TKey, TInstance>
    where TKey : notnull
{
    public static PowerOfTwo DefaultSize { get; } = new PowerOfTwo(32);
    private readonly int poolIndexBitShift;
    protected ImmutableArray<TInstance> Pool { get; }

    public PerKeyPool(Func<TInstance> factory)
        : this(DefaultSize, factory) { }

    public PerKeyPool(PowerOfTwo poolSize, Func<TInstance> factory)
    {
        if (poolSize.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), poolSize, "Pool size has to be bigger then 0.");
        }

        var poolBuilder = ImmutableArray.CreateBuilder<TInstance>((int)poolSize.Value);
        for (int index = 0; index < Pool.Length; index++)
        {
            poolBuilder.Add(factory());
        }
        Pool = poolBuilder.MoveToImmutable();
        poolIndexBitShift = (sizeof(int) * 8) - BitOperations.TrailingZeroCount(Pool.Length);
    }

    public TInstance Get(TKey key)
        => Pool[GetIndex(key)];

    private int GetIndex(TKey key)
        => (int)(Hashing.Fibonacci(key) >> poolIndexBitShift);
}

public class PerKeyDisposablePoolV2<TKey, TInstance>
    : PerKeyPool<TKey, TInstance>, IDisposable
    where TInstance : IDisposable
    where TKey : notnull
{
    private bool disposedValue;

    public PerKeyDisposablePoolV2(Func<TInstance> factory) : base(factory) { }

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

public class Test
{
    public Test()
    {
        using var pool = new PerKeySynchronizerV3<Guid>();
        pool.Get(Guid.NewGuid());
    }
}