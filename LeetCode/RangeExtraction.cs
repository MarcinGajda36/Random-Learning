namespace LeetCode;
using System;
using System.Collections.Generic;
using System.Text;

public class RangeExtraction
{
    public static string Extract(int[] args)
    {
        if (args.Length == 0)
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
                AppendRange(extracted, currentRange);
                currentRange.Clear();
            }

            currentRange.Add(currentElement);
            leftToExtract = leftToExtract[1..];
        }

        AppendRange(extracted, currentRange);
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
        if (range.Count == 0)
        {
            return destination;
        }

        if (destination.Length > 0)
        {
            destination.Append(',');
        }

        return range switch
        {
            [var one] => destination.Append(one),
            [var one, var two] => destination.Append($"{one},{two}"),
            [var first, .., var last] => destination.Append($"{first}-{last}"),
        };
    }
}