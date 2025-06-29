namespace MarcinGajda.GenericsTests;
using System;
using System.Numerics;

public static class MyTryAdd
{
    // For overflow, integers are checked/unchecked dependent, floating points always saturate, decimal always throws
    public enum AddIntegerResult
    {
        Success,
        Overflow,
    }
    public static (T, AddIntegerResult) TryAddInteger<T>(T left, T right)
        where T : IBinaryInteger<T>
    {
        try
        {
            return (checked(left + right), AddIntegerResult.Success);
        }
        catch (OverflowException)
        {
            return (T.Zero, AddIntegerResult.Overflow);
        }
    }

    public enum AddFloatingResult
    {
        Success,
        Saturation,
    }
    public static (T, AddIntegerResult) TryAddFloating<T>(T left, T right)
        where T : IFloatingPoint<T>
    {
        return (checked(left + right), AddIntegerResult.Success); // This is bad, if i add float.MaxValue + 5 i get float.MaxValue and no exception
    }
}
