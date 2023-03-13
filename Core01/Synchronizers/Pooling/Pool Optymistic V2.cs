using System;
using System.Threading;

namespace MarcinGajda.Synchronizers.Pooling;
public class SpiningPoolV2<TValue> where TValue : class
{
    public struct Lease : IDisposable
    {
        readonly SpiningPoolV2<TValue> parent;
        TValue? value;
        public TValue Value => value ?? throw new ObjectDisposedException(nameof(Lease));

        public Lease(TValue value, SpiningPoolV2<TValue> parent)
        {
            this.value = value;
            this.parent = parent;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref value, null) is TValue noNull)
            {
                parent.Return(noNull);
            }
        }
    }

    readonly Func<TValue> factory;
    readonly TValue?[] pool;

    volatile int returnIndex;
    volatile int rentIndex;

    public SpiningPoolV2(int size, Func<TValue> factory)
    {
        this.factory = factory;
        pool = new TValue?[size];
    }

    public Lease Rent()
    {
        SpinWait spinWait = default;
        while (true)
        {
            int rentIdx = rentIndex;
            if (Interlocked.Exchange(ref pool[rentIdx], null) is TValue value)
            {
                return new(value, this);
            }
            if (rentIdx != returnIndex)
            {
                Interlocked.CompareExchange(ref rentIndex, GetNextIndex(rentIdx), rentIdx);
                spinWait.SpinOnce();
                continue;
            }
            break;
        }
        return new(factory(), this);
    }

    private int GetNextIndex(int currentIndex)
    {
        var next = currentIndex + 1;
        if (next == pool.Length)
        {
            return 0;
        }

        return next;
    }

    void Return(TValue value)
    {
        SpinWait spinWait = default;
        while (true)
        {
            int returnIdx = returnIndex;
            if (Interlocked.CompareExchange(ref pool[returnIdx], value, null) == null)
            {
                return;
            }
            if (returnIdx != GetLastIndexBefore(rentIndex))
            {
                Interlocked.CompareExchange(ref returnIndex, GetNextIndex(returnIdx), returnIdx);
                spinWait.SpinOnce();
                continue;
            }
            return;
        }
    }

    private int GetLastIndexBefore(int before)
    {
        if (before == 0)
        {
            return pool.Length - 1;
        }
        return before - 1;
    }
}