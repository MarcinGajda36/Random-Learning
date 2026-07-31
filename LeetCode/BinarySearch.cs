namespace LeetCode;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

public class BinarySearch
{
    public static int FindIndexOf01<TElement>(IReadOnlyList<TElement> haystack, TElement toFind)
    {
        if (haystack is not { Count: > 0 })
            return -1;

        var lowerLimit = 0;
        var upperLimit = haystack.Count - 1;
        while (lowerLimit <= upperLimit && upperLimit >= lowerLimit)
        {
            var indexToCheck = (upperLimit + lowerLimit) / 2;
            var candidate = haystack[indexToCheck];
            switch (Comparer<TElement>.Default.Compare(candidate, toFind))
            {
                case > 0:
                    upperLimit = indexToCheck - 1;
                    break;
                case 0:
                    return indexToCheck;
                case < 0:
                    lowerLimit = indexToCheck + 1;
                    break;
            }
        }

        return -1;
    }

    public static int FindIndexOf02<TElement>(IReadOnlyList<TElement> haystack, TElement toFind)
    {
        ArgumentNullException.ThrowIfNull(haystack);
        const int NotFound = -1;
        return haystack switch
        {
            // I like how switches help me think through cases 
            [] => NotFound,
            [var one] => FineInOne(one, toFind),
            TElement[] array => FindInManySpan(array, toFind),
            List<TElement> list => FindInManySpan(CollectionsMarshal.AsSpan(list), toFind),
            ImmutableArray<TElement> array => FindInManySpan(array.AsSpan(), toFind),
            var many => FindInManyGeneric(many, toFind),
        };

        static int FineInOne(TElement one, TElement toFind)
            => Comparer<TElement>.Default.Compare(one, toFind) == 0 ? 0 : NotFound;

        static int FindInManyGeneric(IReadOnlyList<TElement> many, TElement toFind)
        {
            var lowerLimit = 0;
            var upperLimit = many.Count - 1;
            while (lowerLimit <= upperLimit)
            {
                var indexToCheck = (upperLimit + lowerLimit) / 2;
                var candidate = many[indexToCheck];
                switch (Comparer<TElement>.Default.Compare(candidate, toFind))
                {
                    case > 0:
                        upperLimit = indexToCheck - 1;
                        break;
                    case 0:
                        return indexToCheck;
                    case < 0:
                        lowerLimit = indexToCheck + 1;
                        break;
                }
            }

            return NotFound;
        }

        static int FindInManySpan(ReadOnlySpan<TElement> many, TElement toFind)
        {
            // Idea was to compare vector to vector centered around the index Binary search would choose
            // Problems
            // 1) if TElement is not supported then this will throw
            // 2) Vector's only have IndexOf, no compare
            var lowerLimit = 0;
            var upperLimit = many.Length - 1;
            while (lowerLimit <= upperLimit)
            {
                var indexToCheck = (upperLimit + lowerLimit) / 2;
                var candidate = many[indexToCheck];
                switch (Comparer<TElement>.Default.Compare(candidate, toFind))
                {
                    case > 0:
                        upperLimit = indexToCheck - 1;
                        break;
                    case 0:
                        return indexToCheck;
                    case < 0:
                        lowerLimit = indexToCheck + 1;
                        break;
                }
            }

            return NotFound;
        }
    }
}
