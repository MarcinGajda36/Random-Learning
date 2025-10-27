using System;
using System.Linq;

namespace SpansAndStuff;

internal class ArraySegments
{
    public static void Test()
    {
        int[] ints = [1, 2, 3, 4, 5];

        int[] fstSeg = ints[0..2];
    }

    public static void Test4()
    {
        Span<int> sp = Enumerable.Range(0, 10).ToArray();
        while (sp.Length > 1)
        {
            for (int i = 1; i < sp.Length; i++)
            {
                bool areEqual = sp[0] == sp[i];
            }
            sp = sp.Slice(1);
        }
    }
}
