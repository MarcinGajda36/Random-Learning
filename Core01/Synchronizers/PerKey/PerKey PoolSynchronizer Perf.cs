using System;
using System.Buffers;
using System.Collections.Generic;
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
    private readonly SemaphoreSlim[] pool;
    private readonly int poolIndexBitShift;
    private bool disposedValue;

    public PoolPerKeySynchronizerPerf()
        : this(DefaultSize) { }

    public PoolPerKeySynchronizerPerf(PowerOfTwo poolSize)
    {
        if (poolSize.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(poolSize), poolSize, "Pool size has to be power of 2 and bigger then 0.");
        }
        poolIndexBitShift = (sizeof(int) * 8) - BitOperations.TrailingZeroCount(poolSize.Value);
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
        IReadOnlyCollection<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        static void ReleaseLocked(SemaphoreSlim[] pool, Span<uint> locked)
        {
            for (int index = locked.Length - 1; index >= 0; index--)
            {
                _ = pool[locked[index]].Release();
            }
        }

        var keyIndexes = ArrayPool<uint>.Shared.Rent(keys.Count);
        int keyIndexesCount = FillWithKeyIndexes(keys, keyIndexes);
        keyIndexes.AsSpan(0, keyIndexesCount).Sort();

        for (int index = 0; index < keyIndexesCount; index++)
        {
            try
            {
                await pool[keyIndexes[index]].WaitAsync(cancellationToken);
            }
            catch
            {
                ReleaseLocked(pool, keyIndexes.AsSpan(0, index + 1));
                ArrayPool<uint>.Shared.Return(keyIndexes);
                throw;
            }
        }

        try
        {
            return await resultFactory(argument, cancellationToken);
        }
        finally
        {
            ReleaseLocked(pool, keyIndexes.AsSpan(0, keyIndexesCount));
            ArrayPool<uint>.Shared.Return(keyIndexes);
        }
    }

    private int FillWithKeyIndexes(IReadOnlyCollection<TKey> keys, uint[] keysIndexes)
    {
        int keyCount = 0;
        foreach (var key in keys)
        {
            var keyIndex = GetIndex(key);
            if (keysIndexes.AsSpan(0, keyCount).Contains(keyIndex) is false)
            {
                keysIndexes[keyCount++] = keyIndex;
            }
        }
        return keyCount;
    }

    private uint GetIndex(TKey key)
        // HashFibonacci gives better hash distribution 
        // bit shift needs pool size to be power of 2 to work (alternative is modulo)
        // Fibonacci and bit shift complement each other well for index distribution
        // https://www.youtube.com/watch?v=9XNcbN08Zvc&list=PLqWncHdBPoD4-d_VSZ0MB0IBKQY0rwYLd&index=5
        => Hashing.Fibonacci(key) >> poolIndexBitShift;

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
    private static readonly PoolPerKeySynchronizerPerf<TKey> synchronizer = new();

    public static Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => synchronizer.SynchronizeAsync(key, argument, resultFactory, cancellationToken);
}