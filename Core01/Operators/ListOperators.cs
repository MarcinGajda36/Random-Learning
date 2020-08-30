using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace MarcinGajda.Operators
{
    class ListOperators
    {
        public static void Test()
        {
            var list = new List<int>();
            //var added = list + list;
            var immutableList = ImmutableList.Create<int>();
            //var added = immutableList + immutableList;
            var vector = new Vector<int>();
        }

        public static List<int> Test1(IReadOnlyCollection<int> vs)
        {
            double a = .1;
            foreach (int i in vs.Select(x => x + 123))
            {
                Console.WriteLine(i);
            }

            foreach (int i in vs.Where(x => x % 2 == 0))
            {
                Console.WriteLine(i);
            }
            return default;
        }

    }
}
