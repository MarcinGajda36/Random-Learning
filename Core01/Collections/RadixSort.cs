using System;
using System.Buffers;

namespace MarcinGajda.Collections;
public static class Sort
{
    public static void RadixSort(this uint[] toSort)
    {
        // our helper array 
        var rented = ArrayPool<uint>.Shared.Rent(toSort.Length);
        Span<uint> temp = rented.AsSpan(0, toSort.Length);

        // number of bits our group will be long 
        // try to set this also to 2, 8 or 16 to see if it is 
        // quicker or not 
        const int BitsPerGroup = 4;

        // number of bits of a C# int 
        const int TotalBits = 32;

        // counting and prefix arrays
        // (note dimensions 2^r which is the number of all possible values of a 
        // r-bit number) 
        Span<int> counting = stackalloc int[1 << BitsPerGroup];
        Span<int> prefix = stackalloc int[1 << BitsPerGroup];

        // number of groups 
        int groups = TotalBits / BitsPerGroup;

        // the mask to identify groups 
        uint mask = (1u << BitsPerGroup) - 1u;

        // the algorithm: 
        for (int c = 0, shift = 0; c < groups; c++, shift += BitsPerGroup)
        {
            // reset count array 
            counting.Clear();

            // counting elements of the c-th group 
            for (int i = 0; i < toSort.Length; i++)
            {
                var index = (toSort[i] >> shift) & mask;
                counting[(int)index]++;
            }

            // calculating prefixes 
            prefix[0] = 0;
            for (int i = 1; i < counting.Length; i++)
                prefix[i] = prefix[i - 1] + counting[i - 1];

            // from a[] to t[] elements ordered by c-th group 
            for (int i = 0; i < toSort.Length; i++)
            {
                var prefixIndex = (toSort[i] >> shift) & mask;
                rented[prefix[(int)prefixIndex]++] = toSort[i];
            }

            // a[]=t[] and start again until the last group 
            temp.CopyTo(toSort);
        }
        ArrayPool<uint>.Shared.Return(rented);
    }
}
