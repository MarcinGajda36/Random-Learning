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

    private readonly struct SumOperation<TNumber, TAccumulator> : IOperationOnVectors<TNumber, TAccumulator>
        where TNumber : INumberBase<TNumber>
        where TAccumulator : INumberBase<TAccumulator>
    {
        public static Vector<TNumber> DoVectorized(Vector<TNumber> current, Vector<TNumber> next) => Vector.Add(current, next);
        public static TAccumulator Accumulate(TAccumulator accumulator, TNumber left) => accumulator + TAccumulator.CreateChecked(left);
    }

    public static TResult ForEach<TElement, TResult, TOperation>(
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

    public static TResult SumVectorized<TNumber, TResult>(ReadOnlySpan<TNumber> numbers, TResult initialResult)
        where TNumber : INumberBase<TNumber>
        where TResult : INumberBase<TResult>
        => ForEach<TNumber, TResult, SumOperation<TNumber, TResult>>(numbers, Vector<TNumber>.Zero, initialResult);

    public static TNumber SumVectorized<TNumber>(ReadOnlySpan<TNumber> numbers)
        where TNumber : INumberBase<TNumber>
        => SumVectorized(numbers, TNumber.Zero);

    public static TResult AverageVectorized<TNumber, TResult>(ReadOnlySpan<TNumber> numbers, TResult initialResult)
        where TNumber : INumberBase<TNumber>
        where TResult : INumberBase<TResult>
    {
        var sum = SumVectorized(numbers, initialResult);
        return sum / TResult.CreateChecked(numbers.Length);
    }

    public static double Test()
    {
        ReadOnlySpan<int> numbers = [1, 2, 3, 4, 5, 6];
        var sum = AverageVectorized(numbers, 0d);
        var average = sum / numbers.Length;
        return average;
    }
}
