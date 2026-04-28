namespace LeetCode;

using System;

// https://leetcode.com/problems/longest-substring-without-repeating-characters/
public class LongestSubstringWithoutRepeatingCharacters
{
    // Attempt 3: works and fast 
    // extremally hard to get right, had a lot of bugs and deadlocks, 
    // Runtime Beats 95.62%
    // Memory Beats 96.12%
    public int LengthOfLongestSubstring(string source)
    {
        int longestSoFar = 0;
        var sourceAsSpan = source.AsSpan();
        ReadOnlySpan<char> currentSubstring = [];
        for (var primaryIdx = 0; primaryIdx < sourceAsSpan.Length; primaryIdx++)
        {
            var primaryChar = sourceAsSpan[primaryIdx];
            var duplicationIdx = currentSubstring.IndexOf(primaryChar);
            if (duplicationIdx == -1)
            {
                currentSubstring = sourceAsSpan[(primaryIdx - currentSubstring.Length)..(primaryIdx + 1)];
            }
            else
            {
                longestSoFar = Math.Max(longestSoFar, currentSubstring.Length);
                var duplicationIdxInSource = primaryIdx - currentSubstring.Length + duplicationIdx;
                currentSubstring = sourceAsSpan[(duplicationIdxInSource + 1)..(primaryIdx + 1)];
            }
        }
        longestSoFar = Math.Max(longestSoFar, currentSubstring.Length);
        return longestSoFar;
    }

    // Attempt 2: works but slow
    // idk, had deadlock by not moving primaryIdx in all cases
    // also moved primaryIdx too far, missing substrings in as first try, change to primaryIdx++ fixed it and added computation
    //public int LengthOfLongestSubstring(string haystack)
    //{
    //    int longestSoFar = 0;
    //    var leftToSearch = haystack.AsSpan();
    //    for (var primaryIdx = 0; primaryIdx < leftToSearch.Length; primaryIdx++)
    //    {
    //        ReadOnlySpan<char> currentSubstring = [];
    //        for (var substringOffset = 0; substringOffset + primaryIdx < leftToSearch.Length; substringOffset++)
    //        {
    //            var nextCharIdx = primaryIdx + substringOffset;
    //            var nextChar = leftToSearch[nextCharIdx];
    //            if (currentSubstring.Contains(nextChar))
    //            {
    //                break;
    //            }
    //            else
    //            {
    //                currentSubstring = leftToSearch[primaryIdx..(nextCharIdx + 1)];
    //            }
    //        }
    //        var currentLength = currentSubstring.Length;
    //        longestSoFar = Math.Max(currentLength, longestSoFar);
    //    }
    //    return longestSoFar;
    //}

    // Attempt 1: incomplete
    // idk, caused deadlock bny forgetting 'leftToSearch = rest;' and it feelt harder then it had to be
    // missed cases like dvdwc, where i skipped to next d but vdwc was longest
    // public int LengthOfLongestSubstring(string haystack) {
    //     int longestSoFar = -1;
    //     List<char> currentSubscring = [];
    //     var leftToSearch = haystack.AsSpan();
    //     while(leftToSearch is [var next, .. var rest]) {
    //         leftToSearch = rest;
    //         if(currentSubscring.Contains(next)) {
    //             longestSoFar = Math.Max(longestSoFar, currentSubscring.Count);
    //             currentSubscring.Clear();
    //         } else {
    //             currentSubscring.Add(next);
    //         }
    //     }
    //     longestSoFar = Math.Max(longestSoFar, currentSubscring.Count);
    //     return longestSoFar;
    // }
}
