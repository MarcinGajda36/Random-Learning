﻿using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using MarcinGajda.Synchronization.Synchronizers;

namespace MarcinGajda.Synchronization.PerKey;

public sealed class PerKeyConcurrentExclusiveSynchronizer<TKey>
    where TKey : notnull
{
    private sealed class Synchronizer : IDisposable
    {
        public readonly struct Lease : IDisposable
        {
            private readonly IDisposable refCount;

            public bool IsAcquired { get; }

            public Lease(bool isAcquired, IDisposable refCount)
            {
                IsAcquired = isAcquired;
                this.refCount = refCount;
            }

            public void Dispose() => refCount.Dispose();
        }

        private readonly ConcurrentExclusiveSynchronizer concurrentExclusiveSynchronizer = new();
        private readonly RefCountDisposable refCountDisposable;
        private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers;
        private readonly TKey key;

        public bool AddedToDictionary { get; set; }

        public Synchronizer(ConcurrentDictionary<TKey, Synchronizer> synchronizers, TKey key)
        {
            this.key = key;
            this.synchronizers = synchronizers;
            var keyRemoval = Disposable.Create(this, static @this =>
            {
                if (@this.AddedToDictionary)
                {
                    _ = @this.synchronizers.TryRemove(@this.key, out _);
                }
            });
            refCountDisposable = new RefCountDisposable(keyRemoval);
        }

        public Lease GetLease()
        {
            var refCount = refCountDisposable.GetDisposable();
            bool isAcquired = refCountDisposable.IsDisposed is false;
            return new Lease(isAcquired, refCount);
        }

        public Task<TResult> Run<TResult>(
            OperationType operationType,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
            => operationType switch
            {
                OperationType.Exclusive => concurrentExclusiveSynchronizer.ExclusiveAsync(operation, cancellationToken),
                OperationType.Concurrent => concurrentExclusiveSynchronizer.ConcurrentAsync(operation, cancellationToken),
                var unknown => throw new ArgumentOutOfRangeException(nameof(operationType), unknown, "Unknown value."),
            };

        public void Dispose() => refCountDisposable.Dispose();
    }

    private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers = new();

    public async Task<TResult> SynchronizeAsync<TResult>(
        TKey key,
        OperationType operationType,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        while (cancellationToken.IsCancellationRequested is false)
        {
            if (synchronizers.TryGetValue(key, out var oldSynchronizer))
            {
                using var lease = oldSynchronizer.GetLease();
                if (lease.IsAcquired)
                {
                    return await oldSynchronizer.Run(operationType, operation, cancellationToken);
                }
            }
            else
            {
                using var newSynchronizer = new Synchronizer(synchronizers, key);
                if (synchronizers.TryAdd(key, newSynchronizer))
                {
                    newSynchronizer.AddedToDictionary = true;
                    return await newSynchronizer.Run(operationType, operation, cancellationToken);
                }
            }
        }

        return await Task.FromCanceled<TResult>(cancellationToken);
    }
}