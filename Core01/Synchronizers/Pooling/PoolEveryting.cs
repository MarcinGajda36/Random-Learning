using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers.Pooling;

public sealed class PerKeyDisposablePool<TKey, TInsance>
    : IDisposable
    where TInsance : IDisposable
    where TKey : notnull
{
    public static PowerOfTwo DefaultSize { get; } = new PowerOfTwo(32);
    private readonly TInsance[] pool;
    private readonly int poolIndexBitShift;
    private bool disposedValue;

    public PerKeyDisposablePool(Func<TInsance> factory)
        : this(DefaultSize, factory) { }

    public PerKeyDisposablePool(PowerOfTwo powerOfTwo, Func<TInsance> factory)
    {
        if (powerOfTwo.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(powerOfTwo), powerOfTwo, "Pool size has to be bigger then 0.");
        }

        pool = new TInsance[powerOfTwo.Value];
        for (int index = 0; index < pool.Length; index++)
        {
            pool[index] = factory();
        }
        poolIndexBitShift = sizeof(int) * 8 - BitOperations.TrailingZeroCount(pool.Length);
    }

    public TResult Use<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, TInsance, TResult> resultFactory)
    {
        var insance = pool[GetIndex(key)];
        return resultFactory(key, argument, insance);
    }

    public Task<TResult> UseAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, TInsance, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        var insance = pool[GetIndex(key)];
        return resultFactory(key, argument, insance, cancellationToken);
    }

    private uint GetIndex(TKey key)
        => Hashing.UintFibonacci(key) >> poolIndexBitShift;

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
