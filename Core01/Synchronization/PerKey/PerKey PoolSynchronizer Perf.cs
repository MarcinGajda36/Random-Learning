using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers;

public static class Hashing
{
    private const uint FIBONACCI = 2654435769u; // 2^32 / PHI
    public static uint Fibonacci<TKey>(TKey key)
        where TKey : notnull
    {
        var hash = EqualityComparer<TKey>.Default.GetHashCode(key);
        return unchecked((uint)hash * FIBONACCI);
    }
}

public readonly record struct PowerOfTwo
{
    public uint Value { get; }
    public PowerOfTwo(uint value)
    {
        if (BitOperations.IsPow2(value) is false)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Not a power of 2.");
        }
        Value = value;
    }

    public static PowerOfTwo Create(int powerOfTwo)
        => new(1u << powerOfTwo);

    public static PowerOfTwo RoundUpToPowerOf2(uint toRoundUp)
        => new(BitOperations.RoundUpToPowerOf2(toRoundUp));
}

public sealed partial class PoolPerKeySynchronizerPerf<TKey>
    : IDisposable
    where TKey : notnull
{
    public static PowerOfTwo DefaultSize { get; } = new PowerOfTwo(32);
    private readonly static ArrayPool<int> arrayPool = ArrayPool<int>.Shared;
    private readonly SemaphoreSlim[] pool;
    private bool disposedValue;

    public PoolPerKeySynchronizerPerf()
        : this(DefaultSize) { }

    public PoolPerKeySynchronizerPerf(PowerOfTwo poolSize)
    {
        if (poolSize.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), poolSize, "Pool size has to be power of 2 and bigger then 0.");
        }
        pool = new SemaphoreSlim[poolSize.Value];
        for (int index = 0; index < pool.Length; index++)
        {
            pool[index] = new SemaphoreSlim(1, 1);
        }
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

    public async Task<TResult> SynchronizeManyAsync<TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        static void ReleaseLocked(SemaphoreSlim[] pool, Span<int> locked)
        {
            for (int index = locked.Length - 1; index >= 0; index--)
            {
                _ = pool[locked[index]].Release();
            }
        }

        var keyIndexes = arrayPool.Rent(pool.Length);
        int keyIndexesCount = FillWithKeyIndexes(keys, keyIndexes);
        // We need order to avoid deadlock when:
        // 1) Thread 1 hold keys A and B
        // 2) Thread 2 waits for A and B
        // 3) Thread 3 waits for B and A
        // 4) Thread 1 releases A and B
        // 5) Thread 2 grabs A; Thread 3 grabs B
        // 6) Thread 2 waits for B; Thread 3 waits for A infinitely
        Array.Sort(keyIndexes, 0, keyIndexesCount);

        for (int index = 0; index < keyIndexesCount; index++)
        {
            try
            {
                await pool[keyIndexes[index]].WaitAsync(cancellationToken);
            }
            catch
            {
                ReleaseLocked(pool, keyIndexes.AsSpan(..index));
                arrayPool.Return(keyIndexes);
                throw;
            }
        }

        try
        {
            return await resultFactory(argument, cancellationToken);
        }
        finally
        {
            ReleaseLocked(pool, keyIndexes.AsSpan(..keyIndexesCount));
            arrayPool.Return(keyIndexes);
        }
    }

    // if pool.Length is big then this can be useful
    //private int GetKeysLimit(IEnumerable<TKey> keys)
    //{
    //    if (keys.TryGetNonEnumeratedCount(out var count))
    //    {
    //        return Math.Min(count, pool.Length);
    //    }
    //    return pool.Length;
    //}

    private int FillWithKeyIndexes(IEnumerable<TKey> keys, int[] keysIndexes)
    {
        int keyCount = 0;
        foreach (var key in keys)
        {
            var keyIndex = GetIndex(key);
            if (keysIndexes.AsSpan(..keyCount).Contains(keyIndex) is false)
            {
                keysIndexes[keyCount++] = keyIndex;
            }
            // For crazy amount of keys we can stop if keyCount == pool.Length
            // but it feels like optimizing for worst case
        }
        return keyCount;
    }

    private int GetIndex(TKey key)
    {
        // Both bit shift and bit map needs pool size to be power of 2 to work (alternative is modulo)

        // Path 1
        var hash = EqualityComparer<TKey>.Default.GetHashCode(key);
        var poolIndexBitMap = pool.Length - 1;
        return hash & poolIndexBitMap;

        // Path 2
        // HashFibonacci gives better hash distribution 
        // Fibonacci and bit shift complement each other well for index distribution
        // https://www.youtube.com/watch?v=9XNcbN08Zvc&list=PLqWncHdBPoD4-d_VSZ0MB0IBKQY0rwYLd&index=5
        var fibonacci = Hashing.Fibonacci(key);
        var poolIndexBitShift = (sizeof(int) * 8) - BitOperations.TrailingZeroCount(pool.Length);
        return (int)(fibonacci >> poolIndexBitShift);
    }

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Array.ForEach(pool, semaphore => semaphore.Dispose());
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
    private static readonly PoolPerKeySynchronizerPerf<TKey> synchronizer = new();

    public static Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => synchronizer.SynchronizeAsync(key, argument, resultFactory, cancellationToken);
}