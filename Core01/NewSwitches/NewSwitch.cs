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

    public interface IOperationOnVectors<TElement, TResult>
    {
        static abstract Vector<TElement> DoVectorized(Vector<TElement> current, Vector<TElement> next);
        static abstract TResult Accumulate(TResult accumulator, TElement left);
    }

    private readonly struct SumOperation : IOperationOnVectors<int, int>
    {
        public static Vector<int> DoVectorized(Vector<int> current, Vector<int> next) => Vector.Add(current, next);
        public static int Accumulate(int accumulator, int left) => accumulator + left;
    }

    public static TResult ForEachVectorized<TElement, TResult, TOperation>(
        this ReadOnlySpan<TElement> elements,
        Vector<TElement> initial,
        TResult accumulator)
        where TOperation : struct, IOperationOnVectors<TElement, TResult>
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

    public static int SumVectorized(ReadOnlySpan<int> ints)
        => ForEachVectorized<int, int, SumOperation>(ints, Vector<int>.Zero, 0);
}
