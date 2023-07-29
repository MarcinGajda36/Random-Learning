using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Synchronizers;
public sealed class AtomOptymistic<TValue>
    where TValue : class
{
    private TValue value;
    public TValue Value => Volatile.Read(ref value);
    public AtomOptymistic(TValue initial)
        => value = initial;

    public TValue Map<TArgument>(TArgument argument, Func<TValue, TArgument, TValue> mapper)
    {
        while (true)
        {
            var initial = Value;
            var mapped = mapper(initial, argument);
            if (Interlocked.CompareExchange(ref value, mapped, initial) == initial)
            {
                return mapped;
            }
        }
    }

    public TValue Map(Func<TValue, TValue> mapper)
        => Map(mapper, static (initial, func) => func(initial));

    public async Task<TValue> MapAsync<TArgument>(TArgument argument, Func<TValue, TArgument, Task<TValue>> mapper)
    {
        while (true)
        {
            var initial = Value;
            var mapped = await mapper(initial, argument);
            if (Interlocked.CompareExchange(ref value, mapped, initial) == initial)
            {
                return mapped;
            }
        }
    }

    public Task<TValue> MapAsync(Func<TValue, Task<TValue>> mapper)
        => MapAsync(mapper, static (initial, func) => func(initial));

    public async ValueTask<TValue> MapValueAsync<TArgument>(TArgument argument, Func<TValue, TArgument, ValueTask<TValue>> mapper)
    {
        while (true)
        {
            var initial = Value;
            var mapped = await mapper(initial, argument);
            if (Interlocked.CompareExchange(ref value, mapped, initial) == initial)
            {
                return mapped;
            }
        }
    }

    public ValueTask<TValue> MapValueAsync(Func<TValue, ValueTask<TValue>> mapper)
        => MapValueAsync(mapper, static (initial, func) => func(initial));
}

public static class AtomicInt
{
    // This can hide ref from call site, interesting
    public static int VolatileRead(this ref int value)
        => Volatile.Read(ref value);
}

public sealed class PesimisticAtomicInt : IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    public int Value { get; private set; }

    public PesimisticAtomicInt(int initial = 0)
        => Value = initial;

    public int Swap<TArgument>(TArgument argument, Func<int, TArgument, int> swapper)
    {
        semaphore.Wait();
        try
        {
            Value = swapper(Value, argument);
            return Value;
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public int Swap(Func<int, int> swapper)
        => Swap(swapper, static (initial, func) => func(initial));

    public void Dispose()
        => semaphore.Dispose();
}