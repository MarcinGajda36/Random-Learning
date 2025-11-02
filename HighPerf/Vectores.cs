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
        where TAccumulator : INumberBase<TAccumulator>, IAdditionOperators<TAccumulator, TNumber, TAccumulator>
    {
        public static Vector<TNumber> DoVectorized(Vector<TNumber> current, Vector<TNumber> next) => Vector.Add(current, next);
        public static TAccumulator Accumulate(TAccumulator accumulator, TNumber left) => accumulator + left;
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

    public static TNumber SumVectorized<TNumber>(ReadOnlySpan<TNumber> numbers)
        where TNumber : INumberBase<TNumber>
        => ForEach<TNumber, TNumber, SumOperation<TNumber, TNumber>>(numbers, Vector<TNumber>.Zero, TNumber.Zero);

    public static TResult SumVectorized<TNumber, TResult>(ReadOnlySpan<TNumber> numbers, TResult initialResult)
        where TNumber : INumberBase<TNumber>
        where TResult : INumberBase<TResult>, IAdditionOperators<TResult, TNumber, TResult> // This is annoying x1
        => ForEach<TNumber, TResult, SumOperation<TNumber, TResult>>(numbers, Vector<TNumber>.Zero, initialResult);

    public static TResult AverageVectorized<TNumber, TResult>(ReadOnlySpan<TNumber> numbers)
        // This where TNumber is also annoying:
        // 1) i want TResult instead of double
        // 2) if i put double there then average will only work on <double, double>
        where TNumber : INumberBase<TNumber>, IDivisionOperators<TNumber, double, TResult>
        where TResult : INumberBase<TResult>
    {
        var sum = ForEach<TNumber, TNumber, SumOperation<TNumber, TNumber>>(numbers, Vector<TNumber>.Zero, TNumber.Zero);
        return sum / numbers.Length;
    }

    public static double Test()
    {
        ReadOnlySpan<int> numbers = [1, 2, 3, 4, 5, 6];
        var sum = SumVectorized(numbers);
        var average = sum / numbers.Length;
        return average;
    }
}
