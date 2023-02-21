using System;
using System.Threading;

namespace MarcinGajda.Collections;
class Pool<TValue> where TValue : class
{
    public struct Lease : IDisposable
    {
        TValue? value;
        public TValue Value => value ?? throw new ObjectDisposedException(nameof(Lease));

        readonly Pool<TValue> parent;

        public Lease(TValue value, Pool<TValue> parent)
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

    public Pool(int size, Func<TValue> factory)
    {
        this.factory = factory;
        pool = new TValue[size];
    }

    public Lease Rent()
    {
        while (rentIndex != returnIndex)
        {
            int rentIdx = rentIndex;
            int nextRentIndex = GetNextIndex(rentIdx);
            if (Interlocked.Exchange(ref pool[rentIdx], null) is TValue value)
            {
                Interlocked.CompareExchange(ref rentIndex, nextRentIndex, rentIdx);
                return new(value, this);
            }
            Interlocked.CompareExchange(ref rentIndex, nextRentIndex, rentIdx);
        }
        return new(factory(), this);
    }

    private int GetNextIndex(int current)
    {
        var next = current + 1;
        if (next == pool.Length)
        {
            next = 0;
        }

        return next;
    }

    void Return(TValue value)
    {
        var current = returnIndex;
        int next = GetNextIndex(current);
        if (Interlocked.CompareExchange(ref pool[current], value, null) == null)
        {
            Interlocked.CompareExchange(ref returnIndex, next, current);
            return;
        }
    }
}