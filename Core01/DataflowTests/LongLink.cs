using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace MarcinGajda.DataflowTests
{
    internal class LongLink
    {
        private static readonly BufferBlock<int> queue = new BufferBlock<int>();
        private static readonly Dictionary<int, string> hmm = new Dictionary<int, string>();
        public static void Test()
        {

            var transform = new TransformBlock<int, string>(x =>
            {
                var transformed = x.ToString();
                hmm.Add(x, transformed);
                return transformed;
            });

        }
    }
}
