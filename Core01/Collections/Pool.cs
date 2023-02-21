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
            if (Interlocked.CompareExchange(ref pool[toReturn], value, null) == null // Wrong, i can ruin some index forever with this,
                                                                                     // maybe one int that return add rent subtracts?
                && Interlocked.CompareExchange(ref returnIndex, nextToReturn, toReturn) == toReturn)
            {
                return;
            }
        }
    }
}
class LockingPool<TValue>
{
    public struct Lease : IDisposable
    {
        readonly TValue value;
        public TValue Value => isDisposed == AfterDispose
            ? throw new ObjectDisposedException(nameof(Lease))
            : value;

        readonly LockingPool<TValue> parent;
        const int AfterDispose = 1;
        int isDisposed;

        public Lease(TValue value, LockingPool<TValue> parent)
        {
            this.value = value;
            this.parent = parent;
        }

        public void Dispose()
        {
            var temporary = Interlocked.Exchange(ref isDisposed, AfterDispose);
            if (temporary != AfterDispose)
            {
                parent.Return(value);
            }
        }
    }

    readonly Func<TValue> factory;
    readonly TValue[] pool;
    volatile int available;

    public LockingPool(int size, Func<TValue> factory)
    {
        this.factory = factory;
        pool = new TValue[size];
    }

    public Lease Rent()
    {
        if (available == 0)
        {
            return new(factory(), this);
        }

        lock (pool)
        {
            if (available == 0)
            {
                return new(factory(), this);
            }
            available -= 1;
            var toRent = pool[available];
            return new(toRent, this);
        }
    }

    void Return(TValue toReturn)
    {
        if (available == pool.Length - 1)
        {
            return;
        }
        lock (pool)
        {
            if (available == pool.Length - 1)
            {
                return;
            }
            pool[available] = toReturn;
            available += 1;
        }
    }
}
