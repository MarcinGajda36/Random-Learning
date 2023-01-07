using System;
using System.Threading;

namespace MarcinGajda.Synchronizers;
public sealed class OptymisticAtomicInt
{
    private int value;
    public int Value => Volatile.Read(ref value);
    public OptymisticAtomicInt(int initial = 0)
        => value = initial;

    public int Swap<TArgument>(TArgument argument, Func<int, TArgument, int> swapper)
    {
        while (true)
        {
            int initial = Value;
            int swapped = swapper(initial, argument);
            if (Interlocked.CompareExchange(ref value, swapped, initial) == initial)
            {
                return swapped;
            }
        }
    }

    public int Swap(Func<int, int> swapper)
        => Swap(swapper, static (initial, func) => func(initial));
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