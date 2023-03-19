using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronization.Synchronizers;
internal sealed class Limiter : IDisposable
{
    private readonly SemaphoreSlim limiter;

    public Limiter(int limit)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit has to be bigger then 0.");
        }

        limiter = new SemaphoreSlim(limit, limit);
    }

    public async Task<TResult> LimitAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        await limiter.WaitAsync(cancellationToken);
        try
        {
            return await resultFactory(argument, cancellationToken);
        }
        finally
        {
            _ = limiter.Release();
        }
    }

    public Task<TResult> LimitAsync<TResult>(
        Func<CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => LimitAsync(
            resultFactory,
            static (resultFactory, cancellationToken) => resultFactory(cancellationToken),
            cancellationToken);

    public void Dispose()
        => limiter.Dispose();
}
