using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers
{
    public sealed class PerKeySynchronizer<TKey>
        where TKey : notnull
    {
        private sealed class Synchronizer : IDisposable
        {
            public readonly struct LockHolder : IDisposable
            {
                private readonly IDisposable refCount;
                private readonly SemaphoreSlim? toRelease;

                public readonly bool LockAquired;

                public LockHolder(bool lockAquired, IDisposable refCount, SemaphoreSlim? toRelease)
                {
                    LockAquired = lockAquired;
                    this.refCount = refCount;
                    this.toRelease = toRelease;
                }

                public void Dispose()
                {
                    toRelease?.Release();
                    refCount.Dispose();
                }
            }

            private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
            private readonly RefCountDisposable refCountDisposable;
            private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers;
            private readonly TKey key;

            public bool AddedToDictionary;

            public Synchronizer(ConcurrentDictionary<TKey, Synchronizer> synchronizers, TKey key)
            {
                this.synchronizers = synchronizers;
                this.key = key;

                var disposable = Disposable.Create(this, static @this =>
                {
                    if (@this.AddedToDictionary)
                    {
                        @this.synchronizers.TryRemove(@this.key, out _);
                    }
                    @this.semaphoreSlim.Dispose();
                });
                refCountDisposable = new RefCountDisposable(disposable);
            }

            public async ValueTask<LockHolder> GetLockAsync(CancellationToken cancellationToken)
            {
                var refCount = refCountDisposable.GetDisposable();
                if (refCountDisposable.IsDisposed)
                {
                    return new LockHolder(false, refCount, null);
                }
                else
                {
                    await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
                    return new LockHolder(true, refCount, semaphoreSlim);
                }
            }

            public void Dispose()
                => refCountDisposable.Dispose();
        }

        private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers
            = new ConcurrentDictionary<TKey, Synchronizer>();

        public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
            TKey key,
            TArgument argument,
            Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
            CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (synchronizers.TryGetValue(key, out var oldSynchronizer))
                {
                    using var @lock = await oldSynchronizer.GetLockAsync(cancellationToken).ConfigureAwait(false);
                    if (@lock.LockAquired)
                    {
                        return await resultFactory(argument, cancellationToken).ConfigureAwait(false);
                    }
                }
                else
                {
                    using var newSynchronizer = new Synchronizer(synchronizers, key);
                    if (synchronizers.TryAdd(key, newSynchronizer))
                    {
                        newSynchronizer.AddedToDictionary = true;
                        using var @lock = await newSynchronizer.GetLockAsync(cancellationToken).ConfigureAwait(false);
                        return await resultFactory(argument, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            return await Task.FromCanceled<TResult>(cancellationToken).ConfigureAwait(false);
        }
    }
}
