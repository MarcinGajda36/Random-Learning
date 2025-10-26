namespace MarcinGajda.Synchronization.Pooling;
using System;
using System.Runtime.CompilerServices;

public sealed class ThreadStaticPool<TValue>(Func<TValue> factory)
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
        const int ArraySize = 6;
        [InlineArray(ArraySize)]
        struct TValues { TValue first; };

        readonly Func<TValue> factory = parent.factory;
        int available;
        TValues values = new();

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
            if (available != ArraySize)
            {
                values[available++] = value;
            }
        }
    }

    [ThreadStatic]
    static Pool? pool;
    readonly Func<TValue> factory = factory;

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
