using System;
using System.Numerics;

namespace SpansAndStuff;
public static class Vectors
{
    public static void EqualsAny()
    {
        var needle = new Vector<int>(7);
        Span<int> haystackSpan = stackalloc int[8];
        for (int i = 0; i < haystackSpan.Length; i++)
        {
            haystackSpan[i] = i;
        }

        var haystack = new Vector<int>(haystackSpan);
        var equalsAny = Vector.EqualsAny(needle, haystack);
    }
}
