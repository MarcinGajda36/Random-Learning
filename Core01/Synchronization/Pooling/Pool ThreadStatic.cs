using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MarcinGajda.Synchronization.Pooling;
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
                if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
                {
                    ref var toRent = ref values[--available];
                    var value = toRent;
                    toRent = default;
                    return value!;
                }
                return values[--available]!;
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

    public ThreadStaticPool(int sizePerThread, Func<TValue> factory)
    {
        this.sizePerThread = sizePerThread;
        this.factory = factory;
    }

    public Lease Rent()
        => pool is null
        ? new Lease(factory(), this)
        : new Lease(pool.GetOrCreate(), this);

    void Return(TValue value)
    {
        pool ??= new Pool(this);
        pool.Return(value);
    }
}
