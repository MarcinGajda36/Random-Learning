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

public sealed class AtomPessimistic<TValue> : IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);
    public TValue Value { get; private set; }

    public AtomPessimistic(TValue initial)
        => Value = initial;

    public TValue Map<TArgument>(TArgument argument, Func<TValue, TArgument, TValue> mapper)
    {
        semaphore.Wait();
        try
        {
            return Value = mapper(Value, argument);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public TValue Map(Func<TValue, TValue> mapper)
        => Map(mapper, static (initial, func) => func(initial));

    public async Task<TValue> MapAsync<TArgument>(
        TArgument argument,
        Func<TValue, TArgument, Task<TValue>> mapper,
        CancellationToken cancellation = default)
    {
        await semaphore.WaitAsync(cancellation);
        try
        {
            return Value = await mapper(Value, argument);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public async ValueTask<TValue> MapValueAsync<TArgument>(
        TArgument argument,
        Func<TValue, TArgument, ValueTask<TValue>> mapper,
        CancellationToken cancellation = default)
    {
        await semaphore.WaitAsync(cancellation);
        try
        {
            return Value = await mapper(Value, argument);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public void Dispose()
        => semaphore.Dispose();
}