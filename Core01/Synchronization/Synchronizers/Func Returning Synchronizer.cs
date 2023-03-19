using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Synchronizers;
internal class SynchronizationFactory : IDisposable
{
    private readonly SemaphoreSlim semaphore;

    public SynchronizationFactory(int limit)
        => semaphore = new SemaphoreSlim(limit, limit);

    public Func<Func<CancellationToken, Task<TResult>>, Task<TResult>> Create<TResult>(CancellationToken cancellationToken = default)
        => async userFunction =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await userFunction(cancellationToken);
            }
            finally
            {
                _ = semaphore.Release();
            }
        };

    public void Dispose()
        => semaphore.Dispose();
}
