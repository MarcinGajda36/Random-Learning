namespace LeetCode;

using System.Collections.Generic;
using System.Linq;

public class LargestGoodLand
{
    public record XYLength(int StartX, int StartY, int Length);
    public static XYLength LargestGoodSquare(int[][] land)
    {
        List<XYLength> lengths = [];
        for (int y = 0; y < land.Length; y++)
        {
            var row = land[y];
            for (int x = 0; x < row.Length; x++)
            {
                if (row[x] == 0)
                {
                    continue;
                }
                lengths.Add(new(x, y, 1));
                // 011
                // 011
                // 000
                for (var lengthToCheck = 2; x + (lengthToCheck - 1) < row.Length; lengthToCheck++)
                {
                    var offsetToAdd = lengthToCheck - 1;
                    var xToCheck = x + offsetToAdd;
                    if (row[xToCheck] == 1)
                    {
                        // TODO check y’s
                        for (int yOffset = 1; y + yOffset < lengthToCheck; yOffset++)
                        {
                            var rowToCheck = land[y + yOffset];
                            for (int secondaryXOffset = 0; x + secondaryXOffset <= xToCheck; secondaryXOffset++)
                            {
                                if (rowToCheck[x + secondaryXOffset] == 0)
                                {
                                    break;
                                }
                                if (x + secondaryXOffset == xToCheck && yOffset == (lengthToCheck - 1))
                                {
                                    lengths.Add(new(x, y, lengthToCheck));
                                }
                            }
                        }
                    }
                }
            }
        }
        return lengths.Any()
            ? lengths.MaxBy(x => x.Length)
            : new(-1, -1, -1);
    }
}
