using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers;

public enum OperationType
{
    Read = 0,
    Write = 1,
}

public sealed class PerKeyReadWriteSynchronizer<TKey>
    where TKey : notnull
{
    private sealed class Synchronizer : IDisposable
    {
        public readonly record struct Lease : IDisposable
        {
            private readonly IDisposable refCount;

            public bool IsAquired { get; }

            public Lease(bool isAquired, IDisposable refCount)
            {
                IsAquired = isAquired;
                this.refCount = refCount;
            }

            public void Dispose() => refCount.Dispose();
        }

        private readonly ReadWriteSynchronizer readWriteSynchronizer = new();
        private readonly RefCountDisposable refCountDisposable;

        public bool AddedToDictionary { get; set; }

        public Synchronizer(ConcurrentDictionary<TKey, Synchronizer> synchronizers, TKey key)
        {
            var disposable = Disposable.Create(() =>
            {
                if (AddedToDictionary)
                {
                    _ = synchronizers.TryRemove(key, out _);
                }
            });
            refCountDisposable = new RefCountDisposable(disposable);
        }

        public Lease Acquire()
        {
            var refCount = refCountDisposable.GetDisposable();
            return refCountDisposable.IsDisposed
                ? new Lease(false, refCount)
                : new Lease(true, refCount);
        }

        public Task<TResult> Run<TResult>(
            OperationType operationType,
            Func<CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
            => operationType switch
            {
                OperationType.Write => readWriteSynchronizer.Write(operation, cancellationToken),
                OperationType.Read => readWriteSynchronizer.Read(operation, cancellationToken),
                var unknown => throw new ArgumentOutOfRangeException(nameof(operationType), unknown, "Unknown value."),
            };

        public void Dispose() => refCountDisposable.Dispose();
    }

    private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers = new();

    public async Task<TResult> SynchronizeAsync<TResult>(
        TKey key,
        OperationType operationType,
        Func<CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        while (cancellationToken.IsCancellationRequested is false)
        {
            if (synchronizers.TryGetValue(key, out var oldSynchronizer))
            {
                using var lease = oldSynchronizer.Acquire();
                if (lease.IsAquired)
                {
                    return await oldSynchronizer.Run(operationType, resultFactory, cancellationToken);
                }
            }
            else
            {
                using var newSynchronizer = new Synchronizer(synchronizers, key);
                if (synchronizers.TryAdd(key, newSynchronizer))
                {
                    newSynchronizer.AddedToDictionary = true;
                    using var lease = newSynchronizer.Acquire();
                    return await newSynchronizer.Run(operationType, resultFactory, cancellationToken);
                }
            }
        }

        return await Task.FromCanceled<TResult>(cancellationToken).ConfigureAwait(false);
    }
}