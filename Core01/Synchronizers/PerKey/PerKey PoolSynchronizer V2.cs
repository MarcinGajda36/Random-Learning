using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static System.Math;

namespace MarcinGajda.Synchronizers;

public static class Hashing
{
    public static int Fibonacci<TKey>(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        var hash = EqualityComparer<TKey>.Default.GetHashCode(key);
        return hash * 1327217884;
    }

    private const uint fibonacci = 2654435769u; // 2^32 / PHI
    public static uint UintFibonacci<TKey>(TKey key)
        where TKey : notnull
    {
        var hash = EqualityComparer<TKey>.Default.GetHashCode(key);
        return (uint)hash * fibonacci;
    }
}

public readonly record struct PowerOfTwo
{
    public int Value { get; }
    public PowerOfTwo(int value)
    {
        if (IsPowerOf2(value) is false)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Not a power of 2.");
        }
        Value = value;
    }

    public static PowerOfTwo Create(int power)
        => new((int)Pow(2, power));

    public static bool IsPowerOf2(int value)
        => (value & (value - 1)) == 0;

    public static bool IsPowerOf2V2(int value)
        => Ceiling(Log2(value)) - Floor(Log2(value)) < double.Epsilon;
}

public sealed partial class PoolPerKeySynchronizerV2<TKey>
    : IDisposable
    where TKey : notnull
{
    public static PowerOfTwo DefaultSize { get; } = new PowerOfTwo(32);
    private readonly SemaphoreSlim[] pool;
    private readonly int poolIndexBitShift;
    private bool disposedValue;

    public PoolPerKeySynchronizerV2()
        : this(DefaultSize) { }

    public PoolPerKeySynchronizerV2(PowerOfTwo powerOfTwo)
    {
        if (powerOfTwo.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(powerOfTwo), powerOfTwo, "Pool size has to be bigger then 0.");
        }

        pool = new SemaphoreSlim[powerOfTwo.Value];
        for (int index = 0; index < pool.Length; index++)
        {
            pool[index] = new SemaphoreSlim(1, 1);
        }
        poolIndexBitShift = sizeof(int) * 8 - BitOperations.TrailingZeroCount(pool.Length);
    }

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        var semaphore = pool[GetIndex(key)];
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await resultFactory(key, argument, cancellationToken);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    private uint GetIndex(TKey key)
        // HashFibonacci gives better hash distribution + spreads bites all throughout int
        // bit shift needs pool size to be power of 2 to work (alternative is modulo)
        // Fibonacci and bit shift complement each other well for index distribution
        // https://www.youtube.com/watch?v=9XNcbN08Zvc&list=PLqWncHdBPoD4-d_VSZ0MB0IBKQY0rwYLd&index=5
        => Hashing.UintFibonacci(key) >> poolIndexBitShift;

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Array.ForEach(pool, static semaphore => semaphore.Dispose());
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

public static class PerKey<TKey>
    where TKey : notnull
{
    private static readonly PoolPerKeySynchronizerV2<TKey> synchronizer = new();

    public static Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => synchronizer.SynchronizeAsync(key, argument, resultFactory, cancellationToken);
}