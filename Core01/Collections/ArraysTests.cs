using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcinGajda.Collections
{
    public static class ArraysTests
    {
        public static async Task RentTest()
        {
            string[] rented = ArrayPool<string>.Shared.Rent(100);
            try
            {
                //using array here
            }
            finally
            {
                ArrayPool<string>.Shared.Return(rented);
            }
        }
        public static (int, string) Min(IEnumerable<(int?, string)> elements)
        {
            elements.Aggregate((curMin, x) => (curMin == default || (x.Item1 ?? int.MaxValue) <
                curMin.Item1 ? x : curMin));
            return default;
        }

        public static void Test1()
        {
            int[] arr = { 1, 2, 3, 4 };
            int[][] batched1 = arr.Batch(1).ToArray();
            int[][] batched3 = arr.Batch(3).ToArray();
            int[][] batched10 = arr.Batch(10).ToArray();
        }

        public static IEnumerable<T[]> Batch<T>(this T[] array, int size)
        {
            for (int i = 0; i < array.Length; i += size)
            {
                int limit = Math.Min(array.Length, i + size);
                yield return array[i..limit];
            }
        }

        public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> toBatch, int size)
        {
            if (size <= 0) throw new ArgumentException("Size must be bigger then 0", nameof(size));

            var currentBatch = new List<T>(size);
            foreach (T element in toBatch)
            {
                currentBatch.Add(element);
                if (currentBatch.Count == size)
                {
                    yield return currentBatch;
                    currentBatch = new List<T>(size);
                }
            }
            if (currentBatch.Any())
            {
                currentBatch.TrimExcess();
                yield return currentBatch;
            }
        }

        public static void Test()
        {
            int[] toResize = new int[2];
            Array.Resize(ref toResize, toResize.Length + 1);

            Span<int> span = stackalloc[] { 1, 2, 3 };
            for (int i = 0; i < span.Length; i++)
            {
                int atI = span[i];
            }
            foreach (int element in span)
            {

            }

            span.Clear();
            int[] arr = { 4, 5, 6 };
            Memory<int> mem = arr.AsMemory();
        }
        public static void Test3()
        {
            var sList = new SortedList<string, int>();
        }

        public static TResult[] Map<TCollection, TElement, TResult>(this TCollection collection, Func<TElement, TResult> mapper)
            where TCollection : IReadOnlyList<TElement>
        {
            var result = new TResult[collection.Count];
            for (int idx = 0; idx < result.Length; idx++)
            {
                result[idx] = mapper(collection[idx]);
            }
            return result;
        }
    }
}
