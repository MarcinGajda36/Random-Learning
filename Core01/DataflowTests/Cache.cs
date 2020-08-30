using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests
{
    public class Cache<TKey, TVal>
    {
        public Dictionary<TKey, TVal> cached = new Dictionary<TKey, TVal>();
        private readonly ActionBlock<(TKey, TaskCompletionSource<TVal>)> getBlock;
        private readonly ActionBlock<(TKey, TaskCompletionSource<TVal>)> addOrUpdateBlock; //TODO?

        public Cache()
        {
            getBlock = new ActionBlock<(TKey, TaskCompletionSource<TVal>)>(GetOrDownloadVal);

        }

        private async Task GetOrDownloadVal((TKey, TaskCompletionSource<TVal>) keyTcs)
        {
            try
            {
                if (cached.TryGetValue(keyTcs.Item1, out TVal found))
                {
                    keyTcs.Item2.SetResult(found);
                }
                else
                {
                    TVal fetched = await Task.FromResult(default(TVal));//Db select
                    cached.Add(keyTcs.Item1, fetched);
                    keyTcs.Item2.SetResult(fetched);
                }
            }
            catch (Exception ex)
            {
                keyTcs.Item2.SetException(ex);
            }
        }

        public Task<TVal> GetOrFetch(TKey key)
        {
            var tcs = new TaskCompletionSource<TVal>();
            _ = getBlock.Post((key, tcs));
            return tcs.Task;
        }
    }
}
