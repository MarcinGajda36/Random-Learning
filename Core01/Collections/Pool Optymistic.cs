using System;
using System.Threading;

namespace MarcinGajda.Collections;
class OptymisticPool<TValue> where TValue : class
{
    public struct Lease : IDisposable
    {
        TValue? value;
        public TValue Value => value ?? throw new ObjectDisposedException(nameof(Lease));

        readonly OptymisticPool<TValue> parent;

        public Lease(TValue value, OptymisticPool<TValue> parent)
        {
            this.value = value;
            this.parent = parent;
        }

        public void Dispose()
        {
            var temporary = Interlocked.Exchange(ref value, null);
            if (temporary is not null)
            {
                parent.Return(temporary);
            }
        }
    }

    readonly Func<TValue> factory;
    readonly TValue?[] pool;

    volatile int returnIndex;
    volatile int rentIndex;

    public OptymisticPool(int size, Func<TValue> factory)
    {
        this.factory = factory;
        pool = new TValue[size];
    }

    public Lease Rent()
    {
        int rentIdx;
        while ((rentIdx = rentIndex) != returnIndex)
        {
            if (Interlocked.Exchange(ref pool[rentIdx], null) is TValue value)
            {
                Interlocked.CompareExchange(ref rentIndex, GetNextIndex(rentIdx), rentIdx);
                return new(value, this);
            }
        }
        return new(factory(), this);
    }

    private int GetNextIndex(int current)
    {
        var next = current + 1;
        if (next == pool.Length)
        {
            return 0;
        }

        return next;
    }

    void Return(TValue value)
    {
        int returnIdx;
        while ((returnIdx = returnIndex) != LastIndexBefore(rentIndex))
        {
            if (Interlocked.CompareExchange(ref pool[returnIdx], value, null) == null)
            {
                Interlocked.CompareExchange(ref returnIndex, GetNextIndex(returnIdx), returnIdx);
                return;
            }
        }
    }

    private int LastIndexBefore(int before)
    {
        var previous = before - 1;
        if (previous < 0)
        {
            return pool.Length - 1;
        }
        return previous;
    }
}