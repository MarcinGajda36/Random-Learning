using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests
{
    public class DictInBlock<TKey, TVal>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TVal> dict = new Dictionary<TKey, TVal>();
        private readonly ActionBlock<Action<Dictionary<TKey, TVal>>> actionBlock;

        public DictInBlock()
        {
            actionBlock = new ActionBlock<Action<Dictionary<TKey, TVal>>>(action =>
            {
                try
                {
                    action(dict);
                }
                catch
                {
                    //?
                }
            });
        }
        public TVal Get(TKey key)
        {
            TVal val = default;
            _ = actionBlock.Post(dict => dict.TryGetValue(key, out val));
            return val;
        }
    }
}
