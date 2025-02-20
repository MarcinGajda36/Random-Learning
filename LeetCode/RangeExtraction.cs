namespace LeetCode;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public class RangeExtraction
{
    public static string Extract(int[] args)
    {
        if (args is null or [])
        {
            return string.Empty;
        }

        var extracted = new StringBuilder();
        var currentRange = new List<int>();
        var leftToExtract = args.AsSpan();
        while (leftToExtract.Length > 0)
        {
            var currentElement = leftToExtract[0];
            if (IsNextInRange(currentRange, currentElement) is false)
            {
                _ = AppendRange(extracted, currentRange);
                currentRange.Clear();
            }

            currentRange.Add(currentElement);
            leftToExtract = leftToExtract[1..];
        }

        _ = AppendRange(extracted, currentRange);
        return extracted.ToString();
    }

    private static bool IsNextInRange(List<int> range, int candidate)
        => range switch
        {
            [] => true,
            [.., var last] => (last + 1) == candidate,
        };

    private static StringBuilder AppendRange(StringBuilder destination, List<int> range)
    {
        if (range is [])
        {
            return destination;
        }

        if (destination.Length > 0)
        {
            _ = destination.Append(',');
        }

        return range switch
        {
            [] => destination,
            [var one] => destination.Append(one),
            [var one, var two] => destination.Append($"{one},{two}"),
            [var first, .., var last] => destination.Append($"{first}-{last}"),
        };
    }

    private const char Semicolon = ',';
    private static readonly CompositeFormat RangeOfOne = CompositeFormat.Parse("{0}");
    private static readonly CompositeFormat RangeOfTwo = CompositeFormat.Parse("{0},{1}");
    private static readonly CompositeFormat RangeOfThreeOrMore = CompositeFormat.Parse("{0}-{1}");
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static StringBuilder AppendRangeV2(StringBuilder destination, List<int> range)
    {
        // Feels like the worst of all worlds, but it was cool as exercise
        // Why does it feels so bad?
        //  - duplicated list patterns
        //  - a lot of context: fields for CompositeFormat and methods for AppendFormat in contrast to inlined interpolation
        //  - when adding new Range we need to add 2 cases
        return (destination, range) switch
        {
            (_, []) => destination,
            ({ Length: 0 }, [var one]) => AppendRangeOfOne(destination, one),
            ({ Length: 0 }, [var one, var two]) => AppendRangeOfTwo(destination, one, two),
            ({ Length: 0 }, [var first, .., var last]) => AppendRangeOfThreeOrMore(destination, first, last),

            ({ Length: > 0 }, [var one]) => AppendRangeOfOne(destination.Append(Semicolon), one),
            ({ Length: > 0 }, [var one, var two]) => AppendRangeOfTwo(destination.Append(Semicolon), one, two),
            ({ Length: > 0 }, [var first, .., var last]) => AppendRangeOfThreeOrMore(destination.Append(Semicolon), first, last),
        };

        static StringBuilder AppendRangeOfOne(StringBuilder destination, int one)
            => destination.AppendFormat(Invariant, RangeOfOne, one);

        static StringBuilder AppendRangeOfTwo(StringBuilder destination, int one, int two)
            => destination.AppendFormat(Invariant, RangeOfTwo, one, two);

        static StringBuilder AppendRangeOfThreeOrMore(StringBuilder destination, int first, int last)
            => destination.AppendFormat(Invariant, RangeOfThreeOrMore, first, last);
    }
}