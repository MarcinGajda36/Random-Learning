using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.LocksAndSemaphores
{
    public class PerKeySynchronizer<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, ActionBlock<(TaskCompletionSource<TValue>, Func<Task<TValue>>)>> _synchronizers
            = new Dictionary<TKey, ActionBlock<(TaskCompletionSource<TValue>, Func<Task<TValue>>)>>();

        private readonly ActionBlock<(TKey, TaskCompletionSource<TValue>, Func<Task<TValue>>)> Synchronizer;

        public PerKeySynchronizer()
        {
            Synchronizer = new ActionBlock<(TKey key, TaskCompletionSource<TValue> tcs, Func<Task<TValue>> func)>(keyTcsFunc =>
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
            => new ActionBlock<(TaskCompletionSource<TValue> tcs, Func<Task<TValue>> func)>(async tcsFunc =>
            {
                try
                {
                    tcsFunc.tcs.SetResult(await tcsFunc.func().ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    _ = tcsFunc.tcs.TrySetException(ex);
                }
            });

        public Task<TValue> DoAsync(TKey key, Func<Task<TValue>> toDo)
        {
            var tcs = new TaskCompletionSource<TValue>();
            _ = Synchronizer.Post((key, tcs, toDo));
            return tcs.Task;
        }

        public Task<TValue> DoSync(TKey key, Func<TValue> toDo)
            => DoAsync(key, ToTaskFunc(toDo));

        private Func<Task<TValue>> ToTaskFunc(Func<TValue> toDo)
            => () => Task.FromResult(toDo());
    }

    public class PerKeySynchronizer<TKey>
        where TKey : notnull
    {
        private readonly PerKeySynchronizer<TKey, object> perKeySynchronizer = new PerKeySynchronizer<TKey, object>();

        public async Task<TValue> DoAsync<TValue>(TKey key, Func<Task<TValue>> toDo)
            => (TValue)await perKeySynchronizer.DoAsync(key, async () => await toDo().ConfigureAwait(false)).ConfigureAwait(false);

        public async Task<TValue> DoSync<TValue>(TKey key, Func<TValue> toDo)
            => (TValue)await perKeySynchronizer.DoSync(key, () => toDo()).ConfigureAwait(false);
    }

    public static class PerKey<TKey>
        where TKey : notnull
    {
        private static readonly PerKeySynchronizer<TKey> perKeySynchronizer = new PerKeySynchronizer<TKey>();

        public static Task<TValue> DoAsync<TValue>(TKey key, Func<Task<TValue>> toDo)
            => perKeySynchronizer.DoAsync(key, toDo);

        public static Task<TValue> DoSync<TValue>(TKey key, Func<TValue> toDo)
            => perKeySynchronizer.DoSync(key, toDo);
    }
}
