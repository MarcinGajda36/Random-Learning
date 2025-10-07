using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcinGajda.RandomGenerics;

public class GenericsDerivedBaseProblem
{
    public class Base
    {
        public string a = "a";
    }
    public class Derived : Base
    {
    }
    public class Test
    {
        public Base Abc { get; set; }
    }
    public class Test<T>
        : Test
        where T : Base
    {
        public new T Abc { get => (T)base.Abc; set => base.Abc = value; }
    }

    public void TTTTTTTest()
    {
        var a = new Test<Derived> { Abc = new Derived() };
        Test b = a;
        var c = new List<Test<Derived>> { a };
        //List<Test> c1 = c; //nie kompiluje sie 😄
        IEnumerable<Test> c2 = c;//Działa 😮
        Test f = c2.First();
        Base abcq = f.Abc;

        var refLambda = (in int x) => x + 5;
        Func<int, int>? lambda = (int x) => x + 5;
    }


    public class TestRecord(int x) : IEquatable<TestRecord?>
    {
        public int X { get; } = x;

        public override bool Equals(object? obj) => Equals(obj as TestRecord);
        public bool Equals(TestRecord? other) => other is not null && X == other.X;
        public override int GetHashCode()
        {
            var hashCode = new HashCode(); // I wish i could seed it :( 
            hashCode.Add(x);
            return hashCode.ToHashCode();
            return HashCode.Combine(X);
        }

        public static bool operator ==(TestRecord? left, TestRecord? right) => EqualityComparer<TestRecord>.Default.Equals(left, right);
        public static bool operator !=(TestRecord? left, TestRecord? right) => !(left == right);
    }
}
