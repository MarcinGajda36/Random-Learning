using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace HighPerf;

public static class Vectores
{
    public interface IOperationOnVectors<TElement, TResult>
    {
        static abstract Vector<TElement> DoVectorized(Vector<TElement> current, Vector<TElement> next);
        static abstract TResult Accumulate(TResult accumulator, TElement left);
    }

    private readonly struct SumOperation<TNumber> : IOperationOnVectors<TNumber, TNumber>
        where TNumber : INumberBase<TNumber>
    {
        public static Vector<TNumber> DoVectorized(Vector<TNumber> current, Vector<TNumber> next) => Vector.Add(current, next);
        public static TNumber Accumulate(TNumber accumulator, TNumber left) => accumulator + left;
    }

    public static TResult ForEachVectorized<TElement, TResult, TOperation>(
        this ReadOnlySpan<TElement> elements,
        Vector<TElement> initial,
        TResult accumulator)
        where TOperation : IOperationOnVectors<TElement, TResult>
    {
        ref var elementsRef = ref MemoryMarshal.GetReference(elements);
        var offsetToElements = 0;
        for (; offsetToElements <= elements.Length - Vector<TElement>.Count; offsetToElements += Vector<TElement>.Count)
        {
            initial = TOperation.DoVectorized(initial, Vector.LoadUnsafe(ref elementsRef, (nuint)offsetToElements));
        }

        for (var index = 0; index < Vector<TElement>.Count; ++index)
        {
            accumulator = TOperation.Accumulate(accumulator, initial[index]);
        }

        for (var index = offsetToElements; index < elements.Length; ++index)
        {
            accumulator = TOperation.Accumulate(accumulator, elements[index]);
        }

        return accumulator;
    }

    public static TNumber SumVectorized<TNumber>(ReadOnlySpan<TNumber> ints)
        where TNumber : INumberBase<TNumber>
        => ForEachVectorized<TNumber, TNumber, SumOperation<TNumber>>(ints, Vector<TNumber>.Zero, TNumber.Zero);
}
