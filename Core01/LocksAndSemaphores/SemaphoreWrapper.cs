using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.LocksAndSemaphores
{
    public class SafeSemaphoreWrapper : IDisposable
    {
        private readonly SemaphoreSlim semaphoreSlim;

        public SafeSemaphoreWrapper(int initial = 1, int max = 1)
            => semaphoreSlim = new SemaphoreSlim(initial, max);

        public async Task<ReleaseOnDispose> EnterAsync(CancellationToken cancellationToken = default)
        {
            await semaphoreSlim.WaitAsync(cancellationToken);
            return new ReleaseOnDispose(semaphoreSlim);
        }
        public ReleaseOnDispose Enter(CancellationToken cancellationToken = default)
        {
            semaphoreSlim.Wait(cancellationToken);
            return new ReleaseOnDispose(semaphoreSlim);
        }

        public class ReleaseOnDispose : IDisposable
        {
            private SemaphoreSlim semaphoreSlim;

            public ReleaseOnDispose(SemaphoreSlim semaphoreSlim)
            {
                this.semaphoreSlim = semaphoreSlim;
            }

            public void Dispose()
                => Interlocked.Exchange(ref semaphoreSlim, null)?.Release();

            ~ReleaseOnDispose()
            {
                Dispose();
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
            => semaphoreSlim.Dispose();
    }

    public class FastSemaphoreWrapper : IDisposable
    {
        private readonly SemaphoreSlim semaphoreSlim;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FastSemaphoreWrapper(int initial = 1, int max = 1)
            => semaphoreSlim = new SemaphoreSlim(initial, max);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<ReleaseOnDispose> EnterAsync(CancellationToken cancellationToken = default)
        {
            await semaphoreSlim.WaitAsync(cancellationToken);
            return new ReleaseOnDispose(semaphoreSlim);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefReleaseOnDispose Enter(CancellationToken cancellationToken = default)
        {
            semaphoreSlim.Wait(cancellationToken);
            return new RefReleaseOnDispose(semaphoreSlim);
        }

        public readonly struct ReleaseOnDispose : IDisposable
        {
            private readonly SemaphoreSlim _semaphoreSlim;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReleaseOnDispose(SemaphoreSlim semaphoreSlim)
                => _semaphoreSlim = semaphoreSlim;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
                => _semaphoreSlim.Release();
        }
        public readonly ref struct RefReleaseOnDispose
        {
            private readonly SemaphoreSlim _semaphoreSlim;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RefReleaseOnDispose(SemaphoreSlim semaphoreSlim)
                => _semaphoreSlim = semaphoreSlim;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
                => _semaphoreSlim.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
            => semaphoreSlim.Dispose();
    }
}
