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
        while (true)
        {
            var toRent = rentIndex;
            if (toRent == returnIndex)
            {
                return new Lease(factory(), this);
            }
            var nextToRent = toRent + 1;
            if (nextToRent == pool.Length)
            {
                nextToRent = 0;
            }
            if (Interlocked.CompareExchange(ref rentIndex, nextToRent, toRent) == toRent)
            {
                var value = pool[toRent]!;
                pool[toRent] = null;
                return new Lease(value, this);
            }
        }
    }

    void Return(TValue toReturn) // TODO 
    {
        while (true)
        {
            var current = returnIndex;
            if (current == pool.Length - 1)
            {
                return;
            }
            var next = current + 1;
            if (Interlocked.CompareExchange(ref pool[current], toReturn, null) == null
                && Interlocked.CompareExchange(ref returnIndex, next, current) == current)
            {
                return;
            }
        }
    }
}
