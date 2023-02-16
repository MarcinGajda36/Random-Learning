using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.HighPerformance;

namespace HighPerf;

internal class RefOfT
{
    internal class SomeClass
    {
        private int someInt;

        public ref int GetRef()
            => ref someInt;

        //public Ref<int> GetRef1()
        //    => new Ref<int>
    }

}
