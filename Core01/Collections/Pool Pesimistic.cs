using System;
using System.Threading;

namespace MarcinGajda.Collections;
class PesimisticPool<TValue>
{
    public struct Lease : IDisposable
    {
        readonly TValue value;
        public TValue Value => isDisposed == AfterDispose
            ? throw new ObjectDisposedException(nameof(Lease))
            : value;

        readonly PesimisticPool<TValue> parent;
        const int AfterDispose = 1;
        int isDisposed;

        public Lease(TValue value, PesimisticPool<TValue> parent)
        {
            this.value = value;
            this.parent = parent;
        }

        public void Dispose()
        {
            var previousIsDisposed = Interlocked.Exchange(ref isDisposed, AfterDispose);
            if (previousIsDisposed != AfterDispose)
            {
                parent.Return(value);
            }
        }
    }

    readonly Func<TValue> factory;
    readonly TValue[] pool;
    volatile int available;

    public PesimisticPool(int size, Func<TValue> factory)
    {
        this.factory = factory;
        pool = new TValue[size];
    }

    bool IsPoolEmpty()
        => available == 0;

    public Lease Rent()
    {
        if (IsPoolEmpty())
        {
            return new(factory(), this);
        }
        lock (pool)
        {
            if (IsPoolEmpty())
            {
                return new(factory(), this);
            }
            available -= 1;
            var toRent = pool[available];
            return new(toRent, this);
        }
    }

    bool IsPoolFull()
        => available == pool.Length;

    void Return(TValue toReturn)
    {
        if (IsPoolFull())
        {
            return;
        }
        lock (pool)
        {
            if (IsPoolFull())
            {
                return;
            }
            pool[available] = toReturn;
            available += 1;
        }
    }
}