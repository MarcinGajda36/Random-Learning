using System;
using System.Diagnostics.CodeAnalysis;

namespace MarcinGajda.CSarpTypesTests;

public struct Age : IEquatable<Age>
{
    private readonly uint _age;
    private Age(uint age) => _age = age;

    public static Age? Of(uint age)
    {
        if (age < 0 || age > 150)
        {
            return null;
        }
        else
        {
            return new Age(age);
        }
    }

    public override bool Equals(object? obj) => obj is Age age && Equals(age);
    public bool Equals([AllowNull] Age other) => _age == other._age;
    public override int GetHashCode() => HashCode.Combine(_age);

    public static bool operator ==(Age left, Age right) => left.Equals(right);
    public static bool operator !=(Age left, Age right) => !(left == right);

    public static implicit operator uint(Age age) => age._age;
}

public enum EnumTest : byte
{
    Element = 0,
    Element1 = 0,
    Element2 = 0,
    Element3 = 0,
    Element4 = 1,
    Element5 = 2,
    Element6 = 3,
    Element7 = 3,
    Element8 = 255,
    //Element9 = 256, //to much
}
