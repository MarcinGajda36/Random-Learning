namespace MarcinGajda.Synchronization.Pooling;

using System;

public sealed class ThreadStaticPool<TValue>(
    Func<TValue> factory,
    int perThreadSize = 6)
{
    public sealed class Lease(TValue value, ThreadStaticPool<TValue> parent) : IDisposable
    {
        bool isDisposed;

        public TValue Value
        {
            get
            {
                ObjectDisposedException.ThrowIf(isDisposed, this);
                return value;
            }
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

    sealed class Pool(ThreadStaticPool<TValue> parent)
    {
        readonly Func<TValue> factory = parent.factory;
        readonly TValue[] values = new TValue[parent.perThreadSize];
        int available;

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

    [ThreadStatic]
    static Pool? pool;
    readonly Func<TValue> factory = factory;
    readonly int perThreadSize = perThreadSize;

    public TValue Rent()
        => pool is { } notNull
        ? notNull.GetOrCreate()
        : factory();

    public Lease RentLease()
        => new(Rent(), this);

    public void Return(TValue value)
    {
        pool ??= new Pool(this);
        pool.Return(value);
    }
}
