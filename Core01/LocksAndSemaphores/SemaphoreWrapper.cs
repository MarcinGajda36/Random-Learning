using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.LocksAndSemaphores
{
    public class SemaphoreWrapper : IDisposable
    {
        private readonly SemaphoreSlim semaphoreSlim;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SemaphoreWrapper(int initial = 1, int max = 1)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<TResult> EnterAsyncDoSync<TResult>(Func<TResult> toDo, CancellationToken cancellationToken = default)
        {
            using (await EnterAsync(cancellationToken))
                return toDo();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<TResult> DoAsync<TResult>(Func<Task<TResult>> toDo, CancellationToken cancellationToken = default)
        {
            using (await EnterAsync(cancellationToken))
                return await toDo();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task<TResult> DoAsync<TResult>(Func<CancellationToken, Task<TResult>> toDo, CancellationToken cancellationToken = default)
        {
            using (await EnterAsync(cancellationToken))
                return await toDo(cancellationToken);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task EnterAsyncDoSync(Action<CancellationToken> toDo, CancellationToken cancellationToken = default)
        {
            using (await EnterAsync(cancellationToken))
                toDo(cancellationToken);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task EnterAsyncDoSync(Action toDo, CancellationToken cancellationToken = default)
        {
            using (await EnterAsync(cancellationToken))
                toDo();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult DoSync<TResult>(Func<CancellationToken, TResult> toDo, CancellationToken cancellationToken = default)
        {
            using (Enter(cancellationToken))
                return toDo(cancellationToken);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult DoSync<TResult>(Func<TResult> toDo, CancellationToken cancellationToken = default)
        {
            using (Enter(cancellationToken))
                return toDo();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoSync(Action<CancellationToken> toDo, CancellationToken cancellationToken = default)
        {
            using (Enter(cancellationToken))
                toDo(cancellationToken);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DoSync(Action toDo, CancellationToken cancellationToken = default)
        {
            using (Enter(cancellationToken))
                toDo();
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
