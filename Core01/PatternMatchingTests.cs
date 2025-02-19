using MarcinGajda.Structs;

namespace MarcinGajda
{
    internal class PatternMatchingTests
    {
        public int Match<T>(T t) => t switch
        {
            Point { X: 0 } => 1,
            int x => x,
            { } => 1,
            _ => 0
        };

        public static bool IsLowerLetter(char c)
            => c is >= 'a' and <= 'z';
    }
}
