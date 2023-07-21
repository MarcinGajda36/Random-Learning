using System;
using System.Buffers;

namespace MarcinGajda.Collections;
public static class Sort
{
    public static void RadixSort(this uint[] toSort, int bidsInGroup = 8) // 11?
    {
        if (bidsInGroup is < 2 or > 11)
        {
            throw new ArgumentOutOfRangeException(nameof(bidsInGroup), bidsInGroup, "Groups need to be between more then 1 and less then 12");
        }

        // our helper array 
        var tempRent = ArrayPool<uint>.Shared.Rent(toSort.Length);
        Span<uint> temp = tempRent.AsSpan(0, toSort.Length);

        // number of bits our group will be long 
        // try to set this also to 2, 8 or 16 to see if it is 
        // quicker or not 
        const double TotalBits = 32d;
        int GroupsCount = (int)Math.Ceiling(TotalBits / bidsInGroup);
        uint Mask = (1u << bidsInGroup) - 1u;

        // counting and prefix arrays
        // (note dimensions 2^r which is the number of all possible values of a 
        // r-bit number) 
        var elementsCount = 1 << bidsInGroup;
        var countingRent = ArrayPool<int>.Shared.Rent(elementsCount);
        Span<int> counting = countingRent.AsSpan(0, elementsCount);

        var prefixRent = ArrayPool<int>.Shared.Rent(elementsCount);
        Span<int> prefix = prefixRent.AsSpan(0, elementsCount);

        // the algorithm: 
        for (int group = 0, shift = 0; group < GroupsCount; group++, shift += bidsInGroup)
        {
            // reset count array 
            counting.Clear();

            // counting elements of the c-th group 
            for (int i = 0; i < toSort.Length; i++)
            {
                var index = (toSort[i] >> shift) & Mask;
                counting[(int)index]++;
            }

            // calculating prefixes 
            prefix[0] = 0;
            for (int i = 1; i < counting.Length; i++)
                prefix[i] = prefix[i - 1] + counting[i - 1];

            // from a[] to t[] elements ordered by c-th group 
            for (int i = 0; i < toSort.Length; i++)
            {
                var prefixIndex = (toSort[i] >> shift) & Mask;
                tempRent[prefix[(int)prefixIndex]++] = toSort[i];
            }

            // a[]=t[] and start again until the last group 
            temp.CopyTo(toSort);
        }
        ArrayPool<int>.Shared.Return(prefixRent);
        ArrayPool<int>.Shared.Return(countingRent);
        ArrayPool<uint>.Shared.Return(tempRent);
    }
}
