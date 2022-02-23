using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers
{
    public sealed class PoolPerKeySynchronizer<TKey> : IDisposable
        where TKey : notnull
    {
        private readonly SemaphoreSlim[] pool;
        private bool disposedValue;

        public PoolPerKeySynchronizer()
        {
            pool = new SemaphoreSlim[Environment.ProcessorCount];
            for (int index = 0; index < pool.Length; index++)
            {
                pool[index] = new SemaphoreSlim(1);
            }
        }

        public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
            TKey key,
            TArgument argument,
            Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
            CancellationToken cancellationToken = default)
        {
            uint index = (uint)key.GetHashCode() % (uint)pool.Length;
            var semaphore = pool[(int)index];
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await resultFactory(argument, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                semaphore.Release();
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Array.ForEach(pool, semaphore => semaphore.Dispose());
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
