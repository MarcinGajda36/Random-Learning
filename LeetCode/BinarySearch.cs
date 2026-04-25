namespace LeetCode;

using System.Collections.Generic;

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
        const int NotFound = -1;
        return haystack switch
        {
            // I like how switches help me think through cases 
            null or [] => NotFound,
            [var one] => Comparer<TElement>.Default.Compare(one, toFind) == 0 ? 0 : NotFound,
            var many => FindInMany(many, toFind),
        };

        static int FindInMany(IReadOnlyList<TElement> haystack, TElement toFind)
        {
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

            return NotFound;
        }
    }
}
