using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace MarcinGajda.Collections
{
    public static class CollectionsExtentions
    {
        public static TResult[] Map<TElement, TResult>(this TElement[] array, Func<TElement, TResult> mapper)
        {
            var arr = new TResult[array.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = mapper(array[i]);
            }
            return arr;
        }
        public static void Test()
        {
            IEnumerable<IEnumerable<int>> many = Enumerable.Range(0, 10).Select(i => Enumerable.Range(0, i));
            var flatten =
                from collection in many
                from element in collection
                select element;

        }
    }
}
