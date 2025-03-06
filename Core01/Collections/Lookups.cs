using System.Collections.Generic;
using System.Linq;

namespace MarcinGajda.Collections;

public class Lookups
{
    public static void Test()
    {
        ILookup<int, int> lookup = Enumerable.Range(0, 10)
            .ToLookup(x => x % 3);

        IEnumerable<int> dividableBy3 = lookup[0];

        foreach (IGrouping<int, int> group in lookup)
        {

        }
    }
}
