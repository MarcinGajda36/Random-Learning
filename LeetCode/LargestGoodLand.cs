namespace LeetCode;

using System;
using System.Collections.Generic;
using System.Linq;

public class LargestGoodLand01
{
    public record Square(int StartX, int StartY, int Length);
    public static Square LargestGoodSquare(int[][] land)
    {
        List<Square> squares = [];
        for (int startY = 0; startY < land.Length; startY++)
        {
            var startYRow = land[startY];
            for (int startX = 0; startX < startYRow.Length; startX++)
            {
                if (startYRow[startX] == 0)
                {
                    continue;
                }
                squares.Add(new(startX, startY, 1));
                for (var lengthToCheck = 2; startX + (lengthToCheck - 1) < startYRow.Length; lengthToCheck++)
                {
                    var offsetToAdd = lengthToCheck - 1;
                    var startXWithOffset = startX + offsetToAdd;
                    if (startYRow[startXWithOffset] == 0)
                    {
                        break;
                    }

                    if (IsGoodSquare(land, startY, startX, lengthToCheck))
                    {
                        squares.Add(new(startX, startY, lengthToCheck));
                    }
                }
            }
        }
        return squares.Count != 0
            ? squares.MaxBy(x => x.Length)
            : new(-1, -1, -1);
    }

    private static bool IsGoodSquare(int[][] land, int startY, int startX, int lengthToCheck)
    {
        for (int yOffset = 1; startY + yOffset < land.Length; yOffset++)
        {
            var rowToCheck = land[startY + yOffset];
            for (int startXOffset = lengthToCheck - 1; startXOffset >= 0; startXOffset--)
            {
                if (rowToCheck[startX + startXOffset] == 0)
                {
                    return false;
                }

                if (startX + startXOffset == startX && yOffset == (lengthToCheck - 1))
                {
                    return true;
                }
            }
        }
        return false;
    }
}

public class LargestGoodLand02
{
    public record Square(int RightX, int BottomY, int Length);
    public static Square LargestGoodSquare(int[][] land)
    {
        // 1 1 1 1 1 1
        // 0 0 1 1 1 1
        // 0 0 0 1 1 1
        // 0 0 0 0 0 0
        Square largestSoFar = new Square(-1, -1, -1);
        int[][] squaresCache = new int[land.Length][];
        for (int x = 0; x < squaresCache.Length; x++)
        {
            squaresCache[x] = new int[land[x].Length];
        }

        for (int landY = 0; landY < land.Length; landY++)
        {
            var startRow = land[landY];
            for (int landX = 0; landX < startRow.Length; landX++)
            {
                var currentValue = startRow[landX];
                if (currentValue is 0)
                    continue;
                var (left, top, topLeftCorner) = (0, 0, 0);
                var hasLeft = landX > 0;
                var hasTop = landY > 0;
                if (hasLeft)
                    left = squaresCache[landY][landX - 1];
                if (hasLeft && hasTop)
                    topLeftCorner = squaresCache[landY - 1][landX - 1];
                if (hasTop)
                    top = squaresCache[landY - 1][landX];

                var neighboursMin = Math.Min(Math.Min(left, top), topLeftCorner);
                var currentLength = squaresCache[landY][landX] = neighboursMin + 1;

                if (largestSoFar.Length < currentLength)
                {
                    largestSoFar = new Square(landX, landY, currentLength);
                }
            }
        }
        return largestSoFar;
    }
}