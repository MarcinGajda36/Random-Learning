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

}
