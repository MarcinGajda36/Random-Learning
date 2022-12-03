using System;
using System.Threading;

namespace MarcinGajda.Synchronizers;
public sealed class AtomicInt
{
    private int value;
    public int Value => Volatile.Read(ref value);
    public AtomicInt(int initial = 0) => value = initial;

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
