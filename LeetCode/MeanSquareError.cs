namespace LeetCode;
using System;
using System.Linq;

public class Kata
{
    public static double Solution(int[] firstArray, int[] secondArray)
    {
        var length = firstArray.Length;
        var squares = new double[length];
        for (int i = 0; i < length; ++i)
        {
            int difference = Math.Abs(firstArray[i] - secondArray[i]);
            squares[i] = Math.Pow(difference, 2);
        }

        return squares.Average();
    }
}