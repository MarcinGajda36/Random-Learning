using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MarcinGajda.WeakCaches
{
    public static class WeakCache<TKey, TValue>
        where TKey : class
        where TValue : class
    {
        private static readonly ConditionalWeakTable<TKey, TValue> _cache
            = new ConditionalWeakTable<TKey, TValue>();

        public static TValue Cached(TKey key, ConditionalWeakTable<TKey, TValue>.CreateValueCallback valueFactory)
            => _cache.GetValue(key, valueFactory);

        public static async ValueTask<TValue> CachedAsync(TKey key, Func<Task<TValue>> valueFactory)
        {
            if (_cache.TryGetValue(key, out TValue? cached))
            {
                return cached;
            }
            else
            {
                TValue created = await valueFactory().ConfigureAwait(false);
                _cache.AddOrUpdate(key, created);
                return created;
            }
        }
    }
}
