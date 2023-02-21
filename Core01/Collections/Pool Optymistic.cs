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
        pool = new TValue[size + 1];
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
                var value = pool[toRent]!; // hmmm
                pool[toRent] = null;
                return new Lease(value, this);
            }
        }
    }

    void Return(TValue value)
    {
        while (true)
        {
            var toReturn = returnIndex;
            var nextToReturn = toReturn + 1;
            if (nextToReturn == pool.Length)
            {
                nextToReturn = 0;
            }
            if (nextToReturn == rentIndex)
            {
                return;
            }
            if (Interlocked.CompareExchange(ref pool[toReturn], value, null) == null // Is this fine?
                && Interlocked.CompareExchange(ref returnIndex, nextToReturn, toReturn) == toReturn)
            {
                return;
            }
        }
    }
}