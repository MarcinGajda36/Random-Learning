namespace Functional_CS_V2;
internal class C9
{
    public static int Remainder(int dividend, int divisor) => dividend % divisor;

    // (T1, T2) -> TResult
    // T1 -> T2 -> TResult
    public static Func<T1, TResult> ApplyR<T1, T2, TResult>(T2 t2, Func<T1, T2, TResult> func)
        => t1 => func(t1, t2);

    public static Func<int, int> QuotientOf5 = ApplyR<int, int, int>(5, Remainder);

    public static Func<T1, T2, TResult> ApplyR<T1, T2, T3, TResult>(T3 t3, Func<T1, T2, T3, TResult> func)
        => (t1, t2) => func(t1, t2, t3);

}

