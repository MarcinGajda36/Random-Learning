﻿using System;
using System.Threading;

namespace MarcinGajda.Synchronizers.Pooling;
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

    volatile int returnIndex;
    volatile int rentIndex;

    public SpiningPool(int size, Func<TValue> factory)
    {
        this.factory = factory;
        pool = new TValue?[size];
    }

    public Lease Rent()
    {
        SpinWait spinWait = default;
        int rentIdx;
        while ((rentIdx = rentIndex) != returnIndex)
        {
            Interlocked.CompareExchange(ref rentIndex, GetNextIndex(rentIdx), rentIdx);
            if (Interlocked.Exchange(ref pool[rentIdx], null) is TValue value)
            {
                return new(value, this);
            }
            spinWait.SpinOnce();
        }
        return new(factory(), this);
    }

    private int GetNextIndex(int current)
    {
        var next = current + 1;
        if (next == pool.Length)
        {
            return 0;
        }

        return next;
    }

    void Return(TValue value)
    {
        SpinWait spinWait = default;
        int returnIdx;
        while ((returnIdx = returnIndex) != LastIndexBefore(rentIndex))
        {
            Interlocked.CompareExchange(ref returnIndex, GetNextIndex(returnIdx), returnIdx);
            if (Interlocked.CompareExchange(ref pool[returnIdx], value, null) is null)
            {
                return;
            }
            spinWait.SpinOnce();
        }
    }

    private int LastIndexBefore(int before)
    {
        if (before == 0)
        {
            return pool.Length - 1;
        }
        return before - 1;
    }
}