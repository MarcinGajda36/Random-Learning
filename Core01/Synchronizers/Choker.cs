using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers
{
    public class Choker : IDisposable
    {
        private readonly SemaphoreSlim semaphoreSlim;

        public Choker(int initial = 1, int max = 1)
        {
            semaphoreSlim = new SemaphoreSlim(initial, max);
        }

        public T Do<T>(Func<T> func, CancellationToken cancellation = default)
        {
            semaphoreSlim.Wait(cancellation);
            try
            {
                return func();
            }
            finally
            {
                _ = semaphoreSlim.Release();
            }
        }

        public void Do(Action func, CancellationToken cancellation = default)
        {
            semaphoreSlim.Wait(cancellation);
            try
            {
                func();
            }
            finally
            {
                _ = semaphoreSlim.Release();
            }
        }

        public async Task<T> Do<T>(Func<Task<T>> func, CancellationToken cancellation = default)
        {
            await semaphoreSlim.WaitAsync(cancellation);
            try
            {
                return await func();
            }
            finally
            {
                _ = semaphoreSlim.Release();
            }
        }
        public async Task Do(Func<Task> func, CancellationToken cancellation = default)
        {
            await semaphoreSlim.WaitAsync(cancellation);
            try
            {
                await func();
            }
            finally
            {
                _ = semaphoreSlim.Release();
            }
        }

        public void Dispose() => semaphoreSlim.Dispose();

    }
}
