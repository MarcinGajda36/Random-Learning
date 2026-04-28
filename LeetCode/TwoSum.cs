namespace LeetCode;

using System.Collections.Generic;

// https://leetcode.com/problems/two-sum/?envType=company&envId=google&favoriteSlug=google-thirty-days
public class TwoSums
{
    // Attempt 1: brute force O(n^2)
    // public int[] TwoSum(int[] nums, int target) {
    //     for (var primaryIndex = 0; primaryIndex < nums.Length; primaryIndex++) {
    //         var primaryElement = nums[primaryIndex];
    //         var distanceToTarget = target - primaryElement;
    //         for (var secondaryIndex = primaryIndex + 1; secondaryIndex < nums.Length; secondaryIndex++) {
    //             if(distanceToTarget == nums[secondaryIndex]) {
    //                 return [primaryIndex, secondaryIndex];
    //             }
    //         }
    //     }
    //     return [];
    // }

    // Attempt 2: looking for less then O(n^2)
    public int[] TwoSum(int[] nums, int target)
    {
        var indexByDistance = new Dictionary<int, int>();
        for (var currentIdx = 0; currentIdx < nums.Length; currentIdx++)
        {
            var current = nums[currentIdx];
            var distance = target - current;
            if (indexByDistance.TryGetValue(distance, out var previousIdx))
            {
                return [previousIdx, currentIdx];
            }
            indexByDistance[current] = currentIdx;
        }
        return [];
    }
}
