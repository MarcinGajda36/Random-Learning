using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace MarcinGajda.Fs
{
    public static class Exts
    {


        public static T MinBy<T, TR>(this IEnumerable<T> ts, Func<T, TR> func)
            => ListModule.MinBy(FSharpFunc<T, TR>.FromConverter(t => func(t)), WeakCache<T>.GetList(ts));

        private static class WeakCache<T>
        {
            public static readonly ConditionalWeakTable<IEnumerable<T>, FSharpList<T>> cache
                = new ConditionalWeakTable<IEnumerable<T>, FSharpList<T>>();

            public static FSharpList<T> GetList(IEnumerable<T> ts)
                => cache.GetValue(ts, ListModule.OfSeq);

        }
    }
}
