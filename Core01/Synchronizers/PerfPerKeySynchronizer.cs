using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers
{
    public sealed class PerfPerKeySynchronizer<TKey>
    {
        public readonly struct Releaser : IDisposable
        {
            private readonly SemaphoreSlim toRelease;

            public Releaser(SemaphoreSlim toRelease)
                => this.toRelease = toRelease;

            public void Dispose()
                => toRelease?.Release();
        }

        private readonly struct Locker : IDisposable
        {
            private readonly SemaphoreSlim semaphoreSlim;

            public Locker(SemaphoreSlim semaphoreSlim)
                => this.semaphoreSlim = semaphoreSlim;

            public async ValueTask<Releaser> AcquireLock(CancellationToken cancellationToken)
            {
                await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
                return new Releaser(semaphoreSlim);
            }

            public void Dispose()
                => semaphoreSlim.Dispose();
        }

        private sealed class Synchronizer : IDisposable
        {
            public readonly struct LockHolder : IDisposable
            {
                private readonly IDisposable refCount;
                private readonly PerfPerKeySynchronizer<TKey>.Releaser releaser;

                public readonly bool LockAquired;

                public LockHolder(bool lockAquired, IDisposable refCount, Releaser releaser)
                {
                    LockAquired = lockAquired;
                    this.refCount = refCount;
                    this.releaser = releaser;
                }

                public void Dispose()
                {
                    releaser.Dispose();
                    refCount.Dispose();
                }
            }

            private readonly Locker locker = new Locker(new SemaphoreSlim(1));
            private readonly RefCountDisposable refCountDisposable;
            private readonly ConcurrentDictionary<TKey, PerfPerKeySynchronizer<TKey>.Synchronizer> synchronizers;
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
                    @this.locker.Dispose();
                });
                refCountDisposable = new RefCountDisposable(disposable);
            }

            public async ValueTask<LockHolder> GetLock(CancellationToken cancellationToken)
            {
                var refCount = refCountDisposable.GetDisposable();
                if (refCountDisposable.IsDisposed)
                {
                    return new LockHolder(false, refCount, default);
                }
                else
                {
                    var releaser = await locker.AcquireLock(cancellationToken).ConfigureAwait(false);
                    return new LockHolder(true, refCount, releaser);
                }
            }

            public void Dispose()
                => refCountDisposable.Dispose();
        }

        private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers
            = new ConcurrentDictionary<TKey, Synchronizer>();

        public async Task<TResult> SynchronizeAsync<TArguments, TResult>(
            TKey key,
            TArguments arguments,
            Func<TArguments, CancellationToken, Task<TResult>> resultFactory,
            CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (synchronizers.TryGetValue(key, out var synchronizer))
                {
                    using var @lock = await synchronizer.GetLock(cancellationToken).ConfigureAwait(false);
                    if (@lock.LockAquired)
                    {
                        var result = await resultFactory(arguments, cancellationToken).ConfigureAwait(false);
                        return result;
                    }
                }
                else
                {
                    using var newSynchronizer = new Synchronizer(synchronizers, key);
                    if (synchronizers.TryAdd(key, newSynchronizer))
                    {
                        newSynchronizer.AddedToDictionary = true;
                        using var @lock = await newSynchronizer.GetLock(cancellationToken).ConfigureAwait(false);
                        var result = await resultFactory(arguments, cancellationToken).ConfigureAwait(false);
                        return result;
                    }
                }
            }
            return await Task.FromCanceled<TResult>(cancellationToken);
        }
    }
}
