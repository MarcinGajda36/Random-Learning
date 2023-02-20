using System;
using System.Threading;

namespace MarcinGajda.Collections;
class Pool<T> where T : class
{
    public struct Lease : IDisposable
    {
        T? value;
        public T Value => value ?? throw new ObjectDisposedException(nameof(Lease));

        readonly Pool<T> parent;

        public Lease(T value, Pool<T> parent)
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

    readonly Func<T> factory;
    readonly T?[] pool;

    volatile int available;

    public Pool(int size, Func<T> factory)
    {
        this.factory = factory;
        pool = new T[size];
    }

    public Lease Rent()
    {
        while (true)
        {
            var current = available;
            if (current < 1)
            {
                return new Lease(factory(), this);
            }
            var next = current - 1;
            if (Interlocked.CompareExchange(ref available, next, current) == current
                && Interlocked.Exchange(ref pool[current], null) is T oryginal) //TODO is it safe?
            {
                return new Lease(oryginal, this);
            }
        }
    }

    void Return(T toReturn)
    {
        if (available == pool.Length)
        {
            return;
        }
        var current = available;
        var next = current + 1;
        pool[current] = toReturn;
        Interlocked.CompareExchange(ref available, next, current);
    }
}
