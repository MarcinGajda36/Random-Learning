using System;

namespace MarcinGajda.Synchronization.Pooling;
public class ThreadStaticPool<TValue>
{
    public struct Lease : IDisposable
    {
        readonly TValue value;
        readonly ThreadStaticPool<TValue> parent;
        bool isDisposed;

        public readonly TValue Value => isDisposed
            ? throw new ObjectDisposedException(nameof(Lease))
            : value;

        public Lease(TValue value, ThreadStaticPool<TValue> parent)
        {
            this.value = value;
            this.parent = parent;
        }

        public void Dispose()
        {
            if (isDisposed is false)
            {
                parent.Return(value);
                isDisposed = true;
            }
        }
    }

    class Pool
    {
        readonly TValue?[] values;
        readonly Func<TValue> factory;
        int available;

        public Pool(ThreadStaticPool<TValue> parent)
        {
            values = new TValue?[parent.sizePerThread];
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
    readonly int sizePerThread;
    readonly Func<TValue> factory;

    public ThreadStaticPool(Func<TValue> factory, int sizePerThread = 4) // i feel like putting 7 here idk why.
    {
        this.sizePerThread = sizePerThread;
        this.factory = factory;
    }

    public TValue Rent()
        => pool is null
        ? factory()
        : pool.GetOrCreate();

    public Lease RentLease()
        => new(Rent(), this);

    public void Return(TValue value)
    {
        pool ??= new Pool(this);
        pool.Return(value);
    }
}
