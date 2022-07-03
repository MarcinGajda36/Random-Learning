using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests
{
    public class Cache<TKey, TVal>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TVal> cache = new Dictionary<TKey, TVal>();
        private readonly ActionBlock<Command> commandBlock;

        public Cache() => commandBlock
            = new ActionBlock<Command>(command =>
            {
                if (command is GetOrAddCommand getOrAdd)
                {
                    GetOrAdd(getOrAdd);
                }
            });

        public Task<TVal> GetOrAdd(TKey key, Func<TKey, TVal> func)
        {
            //hmmm with immutable dictionary i could first check here 
            var getOrAddCommand = new GetOrAddCommand(key, func);
            _ = commandBlock.Post(getOrAddCommand);
            return getOrAddCommand.Result.Task;
        }

        private void GetOrAdd(GetOrAddCommand getOrAddCommand)
        {
            if (cache.TryGetValue(getOrAddCommand.Key, out var existing))
            {
                getOrAddCommand.Result.SetResult(existing);
            }
            else
            {
                try
                {
                    TVal val = getOrAddCommand.Func(getOrAddCommand.Key);
                    getOrAddCommand.Result.SetResult(val);
                    cache.Add(getOrAddCommand.Key, val);
                }
                catch (Exception ex)
                {
                    getOrAddCommand.Result.TrySetException(ex);
                }
            }
        }

        private abstract class Command
        {
            public abstract TKey Key { get; }
        }
        private class GetOrAddCommand : Command
        {
            public override TKey Key { get; }
            public TaskCompletionSource<TVal> Result { get; }
            public Func<TKey, TVal> Func { get; }

            public GetOrAddCommand(TKey key, Func<TKey, TVal> func)
            {
                Result = new TaskCompletionSource<TVal>();
                Key = key;
                Func = func;
            }
        }
    }
}
