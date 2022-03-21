using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers
{
    public sealed class Choker : IDisposable
    {
        private readonly SemaphoreSlim semaphoreSlim;
        private bool disposedValue;

        public Choker(int initial = 1, int max = 1)
            => semaphoreSlim = new SemaphoreSlim(initial, max);

        public T Do<T>(Func<CancellationToken, T> func, CancellationToken cancellation = default)
        {
            semaphoreSlim.Wait(cancellation);
            try
            {
                return func(cancellation);
            }
            finally
            {
                _ = semaphoreSlim.Release();
            }
        }

        public void Do(Action<CancellationToken> action, CancellationToken cancellation = default)
        {
            semaphoreSlim.Wait(cancellation);
            try
            {
                action(cancellation);
            }
            finally
            {
                _ = semaphoreSlim.Release();
            }
        }

        public async Task<T> DoAsync<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellation = default)
        {
            await semaphoreSlim.WaitAsync(cancellation);
            try
            {
                return await func(cancellation);
            }
            finally
            {
                _ = semaphoreSlim.Release();
            }
        }

        public async Task DoAsync(Func<CancellationToken, Task> func, CancellationToken cancellation = default)
        {
            await semaphoreSlim.WaitAsync(cancellation);
            try
            {
                await func(cancellation);
            }
            finally
            {
                _ = semaphoreSlim.Release();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    semaphoreSlim.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
