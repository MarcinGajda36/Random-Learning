using System.Threading;

namespace MarcinGajda.RandomGenerics;
internal static class ExtentionsPlayground
{
    public static int InInt(this in int x)
        => x;

    public static ref int RefInt(ref this int x)
        => ref x;

    public static T VolatileRead<T>(this T t)
        where T : class
        => Volatile.Read(ref t);

    public static int IDontKnow()
    {
        _ = 1.InInt();
        int x = 1;
        return x.RefInt();
    }
}
