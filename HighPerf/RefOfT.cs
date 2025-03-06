using CommunityToolkit.HighPerformance;

namespace HighPerf;

internal class RefOfT
{
    internal class SomeClass
    {
        private int someInt;

        public ref int GetRef()
            => ref someInt;

        public Ref<int> GetRef1()
            => new(ref someInt);
    }
}
