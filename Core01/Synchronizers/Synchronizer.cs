using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MarcinGajda.Synchronizers
{
    public class Synchronizer : IDisposable
    {

        private readonly SemaphoreSlim semaphoreSlim;
        public Synchronizer(int initial = 1, int max = 1)
            => semaphoreSlim = new SemaphoreSlim(initial, max);

        public Holder GetHolder(in CancellationToken cancellationToken = default)
        {
            semaphoreSlim.Wait(cancellationToken);
            return new Holder(semaphoreSlim);
        }
        public async Task<Holder> GetHolderAsync(CancellationToken cancellationToken = default)
        {
            await semaphoreSlim.WaitAsync(cancellationToken);
            return new Holder(semaphoreSlim);
        }

        public readonly struct Holder : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            internal Holder(in SemaphoreSlim semaphore)
                => _semaphore = semaphore;
            public void Dispose()//Can be called multiple times
                => _ = _semaphore.Release();
        }

        public void Dispose() => semaphoreSlim.Dispose();
    }
    public static class SynchronizerTests
    {
        public static async Task TestAsync()
        {
            using var synchronizer = new Synchronizer();
            using Synchronizer.Holder holder1 = synchronizer.GetHolder(CancellationToken.None);
            using var cancel = new CancellationTokenSource();
            using Synchronizer.Holder holder1_1 = synchronizer.GetHolder(cancel.Token);
            using Synchronizer.Holder holder2 = await synchronizer.GetHolderAsync(CancellationToken.None);
        }
    }
}
