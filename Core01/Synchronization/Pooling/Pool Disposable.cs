using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using MarcinGajda.Synchronization.PerKey;

namespace MarcinGajda.Synchronization.Pooling;

public sealed class PerKeyDisposablePool<TKey, TInstance>
    : IDisposable
    where TInstance : IDisposable
    where TKey : notnull
{
    public static PowerOfTwo DefaultSize { get; } = new PowerOfTwo(32);
    private readonly TInstance[] pool;
    private readonly int poolIndexBitShift;
    private bool disposedValue;

    public PerKeyDisposablePool(Func<TInstance> factory)
        : this(DefaultSize, factory) { }

    public PerKeyDisposablePool(PowerOfTwo powerOfTwo, Func<TInstance> factory)
    {
        if (powerOfTwo.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(powerOfTwo), powerOfTwo, "Pool size has to be bigger then 0.");
        }

        pool = new TInstance[powerOfTwo.Value];
        for (int index = 0; index < pool.Length; index++)
        {
            pool[index] = factory();
        }
        poolIndexBitShift = sizeof(int) * 8 - BitOperations.TrailingZeroCount(pool.Length);
    }

    public TResult Use<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, TInstance, TResult> resultFactory)
    {
        var instance = pool[GetIndex(key)];
        return resultFactory(key, argument, instance);
    }

    public Task<TResult> UseAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, TInstance, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        var instance = pool[GetIndex(key)];
        return resultFactory(key, argument, instance, cancellationToken);
    }

    private uint GetIndex(TKey key)
        => Hashing.Fibonacci(key) >> poolIndexBitShift;

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Array.ForEach(pool, static instance => instance.Dispose());
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
