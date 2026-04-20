namespace LeetCode;

using System.Collections.Generic;
using System.Linq;

public class LargestGoodLand
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
