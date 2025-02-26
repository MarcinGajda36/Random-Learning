//https://leetcode.com/problems/number-of-sub-arrays-with-odd-sum/description/?envType=daily-question&envId=2025-02-25
namespace LeetCode;
using System;
using System.Linq;

public class Solution
{
    /*
        Input: arr = [1,3,5]
        Output: 4
        Explanation: All subarrays are [[1],[1,3],[1,3,5],[3],[3,5],[5]]
        All sub-arrays sum are [1,4,9,3,8,5].
        Odd sums are [1,9,3,5] so the answer is 4.
    */

    // This works correctly but is too slow
    public int NumOfSubarrays(int[] arr)
    {
        var count = 0;
        for (var sliceLength = 1; sliceLength <= arr.Length; ++sliceLength)
        {
            count += CountInSliceLenght(arr, sliceLength);
        }
        return count;
    }

    private static int CountInSliceLenght(int[] source, int length)
    {
        var rangeCount = source.Length - length + 1;
        return Enumerable
            .Range(0, rangeCount)
            .AsParallel()
            .Sum(startIndex =>
            {
                var slice = source.AsSpan(startIndex..(startIndex + length));
                return IsOdd(Sum(slice)) ? 1 : 0;
            });
    }

    private static int Sum(Span<int> toSum)
    {
        var sum = 0;
        foreach (var number in toSum)
        {
            sum += number;
        }
        return sum;
    }

    // This seems like a good start but something is wrong, for [1,2,3,4,5,6,7] should be 16 and this returns 10
    public int NumOfSubarrays1(int[] arr)
    {
        var oddCount = 0;
        for (var index = 0; index < arr.Length; ++index)
        {
            var sum = 0;
            for (var next = index; next < arr.Length; ++next)
            {
                sum += arr[index];
                if (IsOdd(sum))
                {
                    ++oddCount;
                }
            }
        }
        return oddCount;
    }

    private static bool IsOdd(int candidate)
        => (candidate & 1) == 1;

    /*
     * the one below is duplicating
    [1,2,3,4]
    -> [2,3,4]; [1,3,4]; [1,2,4]; [1,2,3]
        -> [2,3,4]; -> [3,4];[2,4]

    public int NumOfSubarrays(int[] arr)
    {
        var count = 0;
        for (var startIndex = 0; startIndex < arr.Length; startIndex++)
        {
            count += NumOfSubArrays(arr, startIndex);
        }
        return count;
    }

    private static int NumOfSubArrays(Span<int> source, int indexToSkip)
    {
        if (source is [])
        {
            return 0;
        }
        else if (source is [var candidate])
        {
            return IsOdd(candidate)
                ? 1
                : 0;
        }

        var count = 0;
        var sourceLength = source.Length;
        Span<int> slice = sourceLength >= 1024
            ? new int[sourceLength - 1]
            : stackalloc int[sourceLength - 1];
        source[0..indexToSkip].CopyTo(slice[0..indexToSkip]);
        source[(indexToSkip + 1)..].CopyTo(slice[indexToSkip..]);

        var sum = 0;
        for (int startIndex = 0; startIndex < slice.Length; ++startIndex)
        {
            sum += slice[startIndex];
        }
        if (IsOdd(sum))
        {
            count += 1;
        }

        for (var startIndex = 0; startIndex < slice.Length; ++startIndex)
        {
            count += NumOfSubArrays(slice, startIndex);
        }
        return count;
    }
    private static bool IsOdd(int candidate)
        => (candidate & 1) == 1;
    */
}