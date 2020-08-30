using System;
using System.Collections.Generic;
using System.Text;
using MarcinGajda.Structsssss;

namespace MarcinGajda
{
    class PatternMatchingTests
    {
        public int Match<T>(T t) => t switch
        {
            Point { X: 0 } => 1,
            int x => x,
            { } => 1,
            _ => 0
        };
    }
}
