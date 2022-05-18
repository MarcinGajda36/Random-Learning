namespace Functional_CS_V2;

internal static class Chapter02
{
    public static Func<T, bool> Negate<T>(this Func<T, bool> toNegate)
        => t => !toNegate(t);

    //public static List<T> Quicksort<T>(List<T> toSort, Comparison<T> comparison)
    //{
    //    for (int i = 0; i < toSort.Count; i += 1)
    //    {

    //    }
    //}
}
