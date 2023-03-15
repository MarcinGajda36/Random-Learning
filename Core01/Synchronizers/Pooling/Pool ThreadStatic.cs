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
            if (Interlocked.Exchange(ref isDisposed, AfterDispose) != AfterDispose)
            {
                parent.Return(value);
            }
        }
    }

    struct Pool
    {
        readonly TValue?[] values;
        readonly Func<TValue> factory;
        int available;

        public Pool(ThreadStaticPool<TValue> parent)
        {
            values = new TValue?[parent.sizePerPool];
            factory = parent.factory;
        }

        public TValue GetOrCreate()
        {
            if (available > 0)
            {
                ref var toRent = ref values[--available];
                var value = toRent;
                toRent = default;
                return value!;
            }
            return factory();
        }

        public void Return(TValue value)
        {
            if (available != values.Length)
            {
                values[available++] = value;
            }
        }
    }

    [ThreadStatic] static Pool? pool;
    readonly int sizePerPool;
    readonly Func<TValue> factory;

    public ThreadStaticPool(int size, Func<TValue> factory)
    {
        sizePerPool = Math.Max(size / Environment.ProcessorCount, 2);
        this.factory = factory;
    }

    public Lease Rent()
    {
        if (pool.HasValue)
        {
            return new Lease(((Pool)pool).GetOrCreate(), this);
        }
        return new Lease(factory(), this);
    }

    void Return(TValue value)
    {
        if (pool.HasValue is false)
        {
            pool = new Pool(this);
        }
        ((Pool)pool).Return(value);
    }
}
