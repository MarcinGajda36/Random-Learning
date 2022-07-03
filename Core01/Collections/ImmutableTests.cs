using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MarcinGajda.Collections
{
    public static class ImmutableTests
    {
        public static ImmutableDictionary<string, int> immutableDictionary = ImmutableDictionary<string, int>.Empty;

        public static int Increment(string key) => ImmutableInterlocked.AddOrUpdate(ref immutableDictionary, key, 1, (k, old) => ++old);
        public static int Decrement(string key) => ImmutableInterlocked.AddOrUpdate(ref immutableDictionary, key, 0, (k, old) => Math.Max(0, --old));
        public static int? Get(string key)
        {
            if (immutableDictionary.TryGetValue(key, out int val))
            {
                return val;
            }
            else
            {
                return null;
            }
        }

        public static void TestArr()
        {
            var arr = ImmutableArray<int>.Empty;
            arr = arr.Add(1);
            ref readonly int element = ref arr.ItemRef(0);
            var build1 = ImmutableArray.CreateBuilder<int>(100);
            var arr2 = build1.ToImmutable();
            var a = new ConcurrentStack<int>();
            var s = new Stack<int>();

            var iarr = ImmutableArray<int>.Empty;
            var iss = ImmutableSortedSet<int>.Empty;
            var ihs = ImmutableHashSet<int>.Empty;
            var idic = ImmutableDictionary<int, string>.Empty;
            var isdic = ImmutableSortedDictionary<int, string>.Empty;
            var ilist = ImmutableList<int>.Empty;
            var iq = ImmutableQueue<int>.Empty;
            var ist = ImmutableStack<int>.Empty;
        }
    }
}
