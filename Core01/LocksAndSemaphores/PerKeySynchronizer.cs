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

        private readonly ActionBlock<(TKey, TaskCompletionSource<TValue>, Func<Task<TValue>>)> _synchronizer;
        private readonly ConcurrentExclusiveSchedulerPair _pair;

        public PerKeySynchronizer(int? degreeOfParalelism = null)
        {
            _pair = new ConcurrentExclusiveSchedulerPair(TaskScheduler.Default, degreeOfParalelism ?? Environment.ProcessorCount);
            _synchronizer = new ActionBlock<(TKey, TaskCompletionSource<TValue>, Func<Task<TValue>>)>(keyTcsFunc =>
            {
                var (key, tcs, func) = keyTcsFunc;
                if (_synchronizers.TryGetValue(key, out var existing))
                {
                    _ = existing.Post((tcs, func));
                }
                else
                {
                    var @new = CreateSynchronizationBlock();
                    _synchronizers.Add(key, @new);
                    _ = @new.Post((tcs, func));
                }
            });
        }

        private ActionBlock<(TaskCompletionSource<TValue>, Func<Task<TValue>>)> CreateSynchronizationBlock()
            => new ActionBlock<(TaskCompletionSource<TValue>, Func<Task<TValue>>)>(static async tcsFunc =>
            {
                var (tcs, func) = tcsFunc;
                try
                {
                    tcs.SetResult(await func().ConfigureAwait(false));
                }
                catch (Exception ex)
                {
                    _ = tcs.TrySetException(ex);
                }
            }, new ExecutionDataflowBlockOptions { TaskScheduler = _pair.ConcurrentScheduler });

        public Task<TValue> DoAsync(TKey key, Func<Task<TValue>> toDo)
        {
            var tcs = new TaskCompletionSource<TValue>();
            _ = _synchronizer.Post((key, tcs, toDo));
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
        private readonly PerKeySynchronizer<TKey, object> _perKeySynchronizer;

        public PerKeySynchronizer(int? degreeOfParalelism = null)
            => _perKeySynchronizer = new PerKeySynchronizer<TKey, object>(degreeOfParalelism);

        public async Task<TValue> DoAsync<TValue>(TKey key, Func<Task<TValue>> toDo)
            => (TValue)await _perKeySynchronizer.DoAsync(key, async () => await toDo().ConfigureAwait(false)).ConfigureAwait(false);

        public async Task<TValue> DoSync<TValue>(TKey key, Func<TValue> toDo)
            => (TValue)await _perKeySynchronizer.DoSync(key, () => toDo()).ConfigureAwait(false);
    }

    public static class PerKey<TKey>
        where TKey : notnull
    {
        private static readonly PerKeySynchronizer<TKey> _perKeySynchronizer = new PerKeySynchronizer<TKey>();

        public static Task<TValue> DoAsync<TValue>(TKey key, Func<Task<TValue>> toDo)
            => _perKeySynchronizer.DoAsync(key, toDo);

        public static Task<TValue> DoSync<TValue>(TKey key, Func<TValue> toDo)
            => _perKeySynchronizer.DoSync(key, toDo);
    }
}
