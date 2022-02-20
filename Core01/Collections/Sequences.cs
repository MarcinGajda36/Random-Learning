using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using LanguageExt;
using static LanguageExt.Prelude;
using Age = System.Int32;

namespace MarcinGajda.Collections
{
    public class Sequences
    {
        public static void Test(Age age)
        {
            Age r = age + 12;
            int r1 = age + 12;
            var readOnlySequence = new ReadOnlySequence<char>();

            var seq = Seq(1, 2, 3);
            seq.Select(x => x * 2);
            WhatArg(seq.AsEnumerable()).ToSeq();
        }
        public static IEnumerable<Age> WhatArg(IEnumerable<Age> vs)
        {
            return vs;
        }
    }
}
