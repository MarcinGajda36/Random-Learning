using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.LocksAndSemaphores
{
    public class PerKeyGenericSynchronizer<TKey>
    {
        private readonly PerKeySynchronizer<TKey, object> perKeySynchronizer = new PerKeySynchronizer<TKey, object>();

        public async Task<TValue> DoAsync<TValue>(TKey key, Func<Task<TValue>> toDo)
            => (TValue)await perKeySynchronizer.Do(key, async () => await toDo().ConfigureAwait(false)).ConfigureAwait(false);

        public async Task<TValue> DoSync<TValue>(TKey key, Func<TValue> toDo)
            => (TValue)await perKeySynchronizer.Do(key, () => Task.FromResult<object>(toDo())).ConfigureAwait(false);
    }

    public class PerKeySynchronizer<TKey, TValue>
    {
        private readonly Dictionary<TKey, ActionBlock<(TaskCompletionSource<TValue>, Func<Task<TValue>>)>> _synchronizers
            = new Dictionary<TKey, ActionBlock<(TaskCompletionSource<TValue>, Func<Task<TValue>>)>>();

        private readonly ActionBlock<(TKey, TaskCompletionSource<TValue>, Func<Task<TValue>>)> Synchronizers;

        public PerKeySynchronizer()
        {
            Synchronizers = new ActionBlock<(TKey key, TaskCompletionSource<TValue> tcs, Func<Task<TValue>> func)>(keyTcsFunc =>
            {
                if (_synchronizers.TryGetValue(keyTcsFunc.key, out var existing))
                {
                    _ = existing.Post((keyTcsFunc.tcs, keyTcsFunc.func));
                }
                else
                {
                    var @new = CreateSynchronizationBlock();
                    _synchronizers.Add(keyTcsFunc.key, @new);
                    _ = @new.Post((keyTcsFunc.tcs, keyTcsFunc.func));
                }
            });
        }

        private static ActionBlock<(TaskCompletionSource<TValue>, Func<Task<TValue>>)> CreateSynchronizationBlock()
            => new ActionBlock<(TaskCompletionSource<TValue>, Func<Task<TValue>>)>(async tcsFunc =>
            {
                try
                {
                    tcsFunc.Item1.SetResult(await tcsFunc.Item2().ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    _ = tcsFunc.Item1.TrySetException(ex);
                }
            });

        public Task<TValue> Do(TKey key, Func<Task<TValue>> toDo)
        {
            var tcs = new TaskCompletionSource<TValue>();
            _ = Synchronizers.Post((key, tcs, toDo));
            return tcs.Task;
        }
    }
}
