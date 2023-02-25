using System;
using System.Threading;

namespace MarcinGajda.Synchronizers.Pooling;
public class ThreadStaticPool<TValue>
{
    public struct Lease : IDisposable
    {
        readonly TValue value;
        public TValue Value => isDisposed == AfterDispose
            ? throw new ObjectDisposedException(nameof(Lease))
            : value;

        readonly ThreadStaticPool<TValue> parent;
        const int AfterDispose = 1;
        int isDisposed;

        public Lease(TValue value, ThreadStaticPool<TValue> parent)
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

    class Pool
    {
        const int Size = 8;
        readonly TValue[] values = new TValue[Size];
        readonly Func<TValue> factory;
        int available;

        public Pool(Func<TValue> factory)
            => this.factory = factory;

        public TValue GetOrCreate()
        {
            if (available > 0)
            {
                available--;
                return values[available];
            }
            return factory();
        }

        public void Return(TValue value)
        {
            if (available == values.Length)
            {
                return;
            }
            values[available] = value;
            available++;
        }
    }

    [ThreadStatic] static Pool? pool;
    readonly Func<TValue> factory;

    public ThreadStaticPool(Func<TValue> factory)
    {
        this.factory = factory;
    }

    public Lease Rent()
    {
        if (pool == null)
        {
            return new Lease(factory(), this);
        }
        return new Lease(pool.GetOrCreate(), this);
    }

    void Return(TValue value)
    {
        pool ??= new Pool(factory);
        pool.Return(value);
    }
}
