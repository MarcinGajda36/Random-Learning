namespace LeetCode;
using System;
using System.Collections.Generic;
using System.Text;

public class RangeExtraction
{
    public static string Extract(int[] args)
    {
        if (args is [])
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
}