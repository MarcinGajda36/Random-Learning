using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers;

public sealed class PerKeySynchronizer<TKey>
    where TKey : notnull
{
    private sealed class Synchronizer : IDisposable
    {
        public readonly struct Lease : IDisposable
        {
            private readonly IDisposable refCount;
            private readonly SemaphoreSlim? toRelease;

            public bool IsAquired { get; }

            public Lease(bool isAquired, IDisposable refCount, SemaphoreSlim? toRelease)
            {
                IsAquired = isAquired;
                this.refCount = refCount;
                this.toRelease = toRelease;
            }

            public void Dispose()
            {
                _ = toRelease?.Release();
                refCount.Dispose();
            }
        }

        private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
        private readonly RefCountDisposable refCountDisposable;
        private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers;
        private readonly TKey key;

        public bool AddedToDictionary { get; set; }

        public Synchronizer(ConcurrentDictionary<TKey, Synchronizer> synchronizers, TKey key)
        {
            this.synchronizers = synchronizers;
            this.key = key;

            var disposable = Disposable.Create(this, static @this =>
            {
                if (@this.AddedToDictionary)
                {
                    _ = @this.synchronizers.TryRemove(@this.key, out _);
                }
                @this.semaphoreSlim.Dispose();
            });
            refCountDisposable = new RefCountDisposable(disposable);
        }

        public async ValueTask<Lease> Acquire(CancellationToken cancellationToken)
        {
            var refCount = refCountDisposable.GetDisposable();
            if (refCountDisposable.IsDisposed)
            {
                return new Lease(false, refCount, null);
            }
            else
            {
                try
                {
                    await semaphoreSlim.WaitAsync(cancellationToken);
                }
                catch
                {
                    refCount.Dispose();
                    throw;
                }
                return new Lease(true, refCount, semaphoreSlim);
            }
        }

        public void Dispose()
            => refCountDisposable.Dispose();
    }

    private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers = new();

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TKey, TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (synchronizers.TryGetValue(key, out var oldSynchronizer))
            {
                using var lease = await oldSynchronizer.Acquire(cancellationToken);
                if (lease.IsAquired)
                {
                    return await resultFactory(key, argument, cancellationToken);
                }
            }
            else
            {
                using var newSynchronizer = new Synchronizer(synchronizers, key);
                if (synchronizers.TryAdd(key, newSynchronizer))
                {
                    newSynchronizer.AddedToDictionary = true;
                    using var lease = await newSynchronizer.Acquire(cancellationToken);
                    return await resultFactory(key, argument, cancellationToken);
                }
            }
        }

        return await Task.FromCanceled<TResult>(cancellationToken);
    }

    public Task<TResult> SynchronizeAsync<TResult>(
        TKey key,
        Func<TKey, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            resultFactory,
            static (key, factory, cancellation) => factory(key, cancellation),
            cancellationToken);
}
