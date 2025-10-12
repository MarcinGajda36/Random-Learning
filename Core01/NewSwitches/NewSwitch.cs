namespace MarcinGajda.NewSwitches;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;

public static class NewSwitch
{
    public static string? Test2(IDictionary<int, string> dictionary, int key) => dictionary switch
    {
        null => null,
        var dict when dict.TryGetValue(key, out var val) => val,
        _ => null,
    };

    public static bool IsPalindrome1(string text)
    {
        // Argument checking notes:
        // 1) I am thinking about ArgumentNullException.ThrowIfNull in public methods and Debug.Assert in private methods
        ArgumentNullException.ThrowIfNull(text);
        return text switch
        {
            // Notes about pattern matching:
            // 1) I like going directly for the case i am interested in first, instead of excluding other cases first,
            // 2) I like making each case 'self contained', it contradicts 'solving a problems you don't have' a bit though
            // 3) I like assigning variable after pattern, it helps with using wrong variable in wrong place, especially with copy-paste
            { Length: > 1 } multiChar => Core(multiChar),
            { Length: <= 1 } => true,
        };

        static bool Core(ReadOnlySpan<char> text)
        {
            for (var i = 0; i < text.Length / 2; ++i)
            {
                if (text[i] != text[^(1 + i)])
                {
                    return false;
                }
            }
            return true;
        }
    }

    public static bool IsPalindrome2(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var leftToCheck = text.AsSpan();
        while (leftToCheck is [var first, .. var middle, var last])
        {
            if (first != last)
            {
                return false;
            }

            leftToCheck = middle;
        }

        return true;
    }

    public static TDestination[] ConvertAll<TSource, TDestination>(
        IEnumerable<TSource> source,
        Func<TSource, TDestination> converter)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(converter);
        return source switch
        {
            TSource[] array => array switch
            {
                [] => [],
                var some => ConvertSpan(some, converter),
            },
            List<TSource> list => list switch
            {
                [] => [],
                var some => ConvertSpan(CollectionsMarshal.AsSpan(some), converter),
            },
            IReadOnlyCollection<TSource> collection => collection switch
            {
                { Count: < 1 } => [],
                var some => ConvertCollection(some, converter)
            },
            var enumerable => ConvertEnumerable(enumerable, converter)
        };

        static TDestination[] ConvertSpan(ReadOnlySpan<TSource> sources, Func<TSource, TDestination> converter)
        {
            var destination = new TDestination[sources.Length];
            for (var index = 0; index < sources.Length; index++)
            {
                destination[index] = converter(sources[index]);
            }
            return destination;
        }

        static TDestination[] ConvertCollection(IReadOnlyCollection<TSource> sources, Func<TSource, TDestination> converter)
        {
            var destination = new TDestination[sources.Count];
            var index = 0;
            foreach (var source in sources)
            {
                destination[index++] = converter(source);
            }
            return destination;
        }

        static TDestination[] ConvertEnumerable(IEnumerable<TSource> sources, Func<TSource, TDestination> converter)
        {
            var destination = ImmutableArray.CreateBuilder<TDestination>();
            foreach (var source in sources)
            {
                destination.Add(converter(source));
            }
            return ImmutableCollectionsMarshal.AsArray(destination.DrainToImmutable())!;
        }
    }

    public interface IOperationOnVectors1<TElement, TResult>
    {
        static abstract Vector<TElement> DoVectorized(Vector<TElement> current, Vector<TElement> next);
        static abstract TResult Accumulate(TResult accumulator, TElement left);
    }

    public static TResult ForEachVectorized1<TElement, TResult, TOperation>(
        this ReadOnlySpan<TElement> elements,
        Vector<TElement> initial,
        TResult accumulator)
        where TOperation : struct, IOperationOnVectors1<TElement, TResult>
    {
        while (elements.Length >= Vector<TElement>.Count)
        {
            initial = TOperation.DoVectorized(initial, new Vector<TElement>(elements));
            elements = elements[Vector<TElement>.Count..];
        }

        for (var index = 0; index < Vector<TElement>.Count; ++index)
        {
            accumulator = TOperation.Accumulate(accumulator, initial[index]);
        }

        for (var index = 0; index < elements.Length; ++index)
        {
            accumulator = TOperation.Accumulate(accumulator, elements[index]);
        }

        return accumulator;
    }

    private readonly struct SumOperation1 : IOperationOnVectors1<int, int>
    {
        public static Vector<int> DoVectorized(Vector<int> current, Vector<int> next) => Vector.Add(current, next);
        public static int Accumulate(int accumulator, int left) => accumulator + left;
    }

    public static int SumVectorized1(ReadOnlySpan<int> ints)
        => ForEachVectorized1<int, int, SumOperation1>(ints, Vector<int>.Zero, 0);

    public interface IOperationOnVectors2<TElement, TResult>
    {
        Vector<TElement> DoVectorized(Vector<TElement> current, Vector<TElement> next);
        TResult Accumulate(TResult accumulator, TElement left);
    }

    public static TResult ForEachVectorized2<TElement, TResult, TOperation>(
        this ReadOnlySpan<TElement> elements,
        Vector<TElement> initial,
        TResult accumulator)
        where TOperation : struct, IOperationOnVectors2<TElement, TResult>
    {
        nuint offsetToElements = 0;
        ref var elementsRef = ref MemoryMarshal.GetReference(elements);
        for (var operationsLeft = elements.Length / Vector<TElement>.Count; operationsLeft > 0; --operationsLeft)
        {
            initial = default(TOperation).DoVectorized(initial, Vector.LoadUnsafe(ref elementsRef, offsetToElements));
            offsetToElements += (nuint)Vector<TElement>.Count;
        }

        for (var index = 0; index < Vector<TElement>.Count; ++index)
        {
            accumulator = default(TOperation).Accumulate(accumulator, initial[index]);
        }

        for (var index = (int)offsetToElements; index < elements.Length; ++index)
        {
            accumulator = default(TOperation).Accumulate(accumulator, elements[index]);
        }

        return accumulator;
    }

    private readonly struct SumOperation2 : IOperationOnVectors2<int, int>
    {
        public Vector<int> DoVectorized(Vector<int> current, Vector<int> next) => Vector.Add(current, next);
        public int Accumulate(int accumulator, int left) => accumulator + left;
    }
    public static int SumVectorized2(ReadOnlySpan<int> ints)
        => ForEachVectorized2<int, int, SumOperation2>(ints, Vector<int>.Zero, 0);
}
