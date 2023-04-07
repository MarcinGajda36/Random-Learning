using System;
using System.Threading;
using MarcinGajda.Synchronization.PerKey;

namespace MarcinGajda.Synchronization.Pooling;
public class SpiningPool<TValue> where TValue : class
{
    public struct Lease : IDisposable
    {
        readonly SpiningPool<TValue> parent;
        TValue? value;
        public TValue Value => value ?? throw new ObjectDisposedException(nameof(Lease));

        public Lease(TValue value, SpiningPool<TValue> parent)
        {
            this.value = value;
            this.parent = parent;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref value, null) is TValue noNull)
            {
                parent.Return(noNull);
            }
        }
    }

    readonly Func<TValue> factory;
    readonly TValue?[] pool;
    readonly int wrapAroundMask;

    int returnIndex;
    int rentIndex;

    public SpiningPool(int size, Func<TValue> factory)
        : this(new PowerOfTwo((uint)size), factory) { }

    public SpiningPool(PowerOfTwo size, Func<TValue> factory)
    {
        if (size.Value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Size has to be bigger then 0.");
        }
        this.factory = factory;
        pool = new TValue?[size.Value];
        wrapAroundMask = pool.Length - 1;
    }

    public Lease Rent()
    {
        SpinWait spinWait = default;
        while (true)
        {
            int rentIdx = Volatile.Read(ref rentIndex);
            if (Interlocked.Exchange(ref pool[rentIdx], null) is TValue value)
            {
                return new(value, this);
            }
            if (rentIdx != Volatile.Read(ref returnIndex))
            {
                Interlocked.CompareExchange(ref rentIndex, GetNextIndex(rentIdx), rentIdx);
                spinWait.SpinOnce();
                continue;
            }
            break;
        }
        return new(factory(), this);
    }

    private int GetNextIndex(int index)
        => (index + 1) & wrapAroundMask;

    void Return(TValue value)
    {
        while (true)
        {
            int returnIdx = Volatile.Read(ref returnIndex);
            if (Interlocked.CompareExchange(ref pool[returnIdx], value, null) == null)
            {
                return;
            }
            if (returnIdx != GetLastIndexBefore(Volatile.Read(ref rentIndex)))
            {
                Interlocked.CompareExchange(ref returnIndex, GetNextIndex(returnIdx), returnIdx);
                continue;
            }
            return;
        }
    }

    private int GetLastIndexBefore(int index)
        => (index - 1) & wrapAroundMask;

}