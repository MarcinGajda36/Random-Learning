using Microsoft.Toolkit.HighPerformance.Buffers;
using Microsoft.Toolkit.HighPerformance.Helpers;
using System;

//This package has been deprecated as it is legacy and is no longer maintained.
//Suggested Alternatives
//CommunityToolkit.HighPerformance

namespace HighPerf;

internal class Program
{
    private static void Main(string[] args)
    {
        _ = Levenshtein.Distance("aaaaa", "aaaab");

        Console.WriteLine("Hello World!");
        using var own = MemoryOwner<int>.Allocate(1);
        _ = own.Span;
        _ = own.Memory;
        using var ownSpan = SpanOwner<int>.Allocate(1);
        _ = ownSpan.Span;

        // Create an array and run the callback
        float[] array = new float[10_000];
        ParallelHelper.ForEach<float, ByTwoMultiplier>(array);
        ParallelHelper.ForEach(array.AsMemory(), new ItemsMultiplier(3.14f));

        int[] arrayInt = new int[10_000];
        ParallelHelper.For(0, array.Length, new ArrayInitializer(arrayInt));
    }
    public readonly struct ByTwoMultiplier : IRefAction<float>
    {
        public void Invoke(ref float x) => x *= 2;
    }
    public readonly struct ItemsMultiplier : IRefAction<float>
    {
        private readonly float factor;

        public ItemsMultiplier(float factor)
            => this.factor = factor;

        public void Invoke(ref float x) => x *= factor;
    }
    public readonly struct ArrayInitializer : IAction
    {
        private readonly int[] array;

        public ArrayInitializer(int[] array)
            => this.array = array;

        public void Invoke(int i)
            => array[i] = i;
    }
}
