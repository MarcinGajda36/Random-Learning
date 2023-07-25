using System;

namespace MarcinGajda.Synchronization.Pooling;
public class LockingPool<TValue>
{
    public struct Lease : IDisposable
    {
        readonly TValue value;
        readonly LockingPool<TValue> parent;
        bool isDisposed;

        public readonly TValue Value => isDisposed
            ? throw new ObjectDisposedException(nameof(Lease))
            : value;

        public Lease(TValue value, LockingPool<TValue> parent)
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

    readonly Func<TValue> factory;
    readonly TValue?[] pool;
    volatile int available;

    bool IsEmpty => available == 0;
    bool IsFull => available == pool.Length;

    public LockingPool(Func<TValue> factory, int size = 32)
    {
        this.factory = factory;
        pool = new TValue?[size];
    }

    public TValue Rent()
    {
        if (IsEmpty)
        {
            return factory();
        }
        lock (pool)
        {
            if (IsEmpty)
            {
                return factory();
            }
            ref var toRent = ref pool[--available];
            var value = toRent;
            toRent = default;
            return value!;
        }
    }

    public Lease RentLease()
        => new(Rent(), this);

    public void Return(TValue toReturn)
    {
        if (IsFull)
        {
            return;
        }
        lock (pool)
        {
            if (IsFull is false)
            {
                pool[available++] = toReturn;
            }
        }
    }
}