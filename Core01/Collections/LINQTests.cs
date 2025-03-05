using System.Collections.Generic;
using System.Linq;

namespace MarcinGajda.Collections
{
    class LINQTests
    {
        public static void Test()
        {
            int[] intArr = [1, 2, 3, 4];
            List<string> stringList = ["a", "b", "c", "d"];

            var join_oneToOne =
                from number in intArr
                join text in stringList on number.ToString() equals text
                select new { number, text };

            var groupJoin_xToMany =
                from number in intArr
                where number % 2 == 1
                join text in stringList on number.ToString() equals text into textGroups
                select new { number, textGroups, /* can't return 'text' here */ };

        }
    }
}
