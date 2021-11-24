using System;
using System.Collections.Concurrent;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers
{
    public sealed class PerKeySynchronizer<TKey>
    {
        private sealed class Locker : IDisposable
        {
            private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);

            public async Task<IDisposable> AcquireLock(CancellationToken cancellationToken)
            {
                await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
                return Disposable.Create(() => semaphoreSlim.Release());
            }

            public void Dispose()
                => Interlocked.Exchange(ref semaphoreSlim, null)?.Dispose();
        }

        private sealed class Synchronizer : IDisposable
        {
            public sealed class LockHolder : IDisposable
            {
                private IDisposable disposable;

                public bool LockAquired { get; }

                private LockHolder(bool lockAquired, IDisposable disposable)
                {
                    LockAquired = lockAquired;
                    this.disposable = disposable;
                }

                public static LockHolder SuccessfulLock(IDisposable disposable)
                    => new LockHolder(true, disposable);

                public static LockHolder FailedLock(IDisposable disposable)
                    => new LockHolder(false, disposable);

                public void Dispose()
                    => Interlocked.Exchange(ref disposable, null)?.Dispose();
            }

            private readonly Locker locker = new Locker();
            private readonly RefCountDisposable refCountDisposable;
            public bool AddedToDictionary { get; set; }

            public Synchronizer(Action removeFromDictionary)
            {
                var disposable = Disposable.Create(() =>
                {
                    if (AddedToDictionary)
                    {
                        removeFromDictionary();
                    }
                    locker.Dispose();
                });
                refCountDisposable = new RefCountDisposable(disposable);
            }

            public async Task<LockHolder> GetLock(CancellationToken cancellationToken)
            {
                var refCount = refCountDisposable.GetDisposable();
                if (refCountDisposable.IsDisposed)
                {
                    return LockHolder.FailedLock(refCount);
                }
                else
                {
                    var @lock = await locker.AcquireLock(cancellationToken).ConfigureAwait(false);
                    return LockHolder.SuccessfulLock(new CompositeDisposable(@lock, refCount));
                }
            }

            public void Dispose()
                => refCountDisposable.Dispose();
        }

        private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers
            = new ConcurrentDictionary<TKey, Synchronizer>();

        public async Task<TResult> SynchronizeAsync<TResult>(
            TKey key,
            Func<Task<TResult>> resultFactory,
            CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var (runSuccessfully, result) = await TrySynchronizeAndRun(key, resultFactory, cancellationToken).ConfigureAwait(false);
                if (runSuccessfully)
                {
                    return result;
                }
            }
            return await Task.FromCanceled<TResult>(cancellationToken);
        }

        private async Task<(bool, TResult)> TrySynchronizeAndRun<TResult>(
            TKey key,
            Func<Task<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            if (synchronizers.TryGetValue(key, out var synchronizer))
            {
                using var @lock = await synchronizer.GetLock(cancellationToken).ConfigureAwait(false);
                if (@lock.LockAquired)
                {
                    var result = await resultFactory().ConfigureAwait(false);
                    return (true, result);
                }
            }
            else
            {
                using var newSynchronizer = new Synchronizer(() => synchronizers.TryRemove(key, out _));
                if (synchronizers.TryAdd(key, newSynchronizer))
                {
                    newSynchronizer.AddedToDictionary = true;
                    using var @lock = await newSynchronizer.GetLock(cancellationToken).ConfigureAwait(false);
                    if (@lock.LockAquired)
                    {
                        var result = await resultFactory().ConfigureAwait(false);
                        return (true, result);
                    }
                }
            }
            return (false, default);
        }
    }
}
