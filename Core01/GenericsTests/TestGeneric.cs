using System;
using System.Collections.Generic;

namespace MarcinGajda.GenericsTests
{
    public delegate TR Map<in T, out TR>(T t);
    public static class TestGeneric
    {
        public static void TestEnum<T>(T t, T flag)
            where T : Enum
        {
            t.HasFlag(flag);
        }

        public static void TestDelegate<TDelegate>(TDelegate @delegate)
            where TDelegate : Delegate
            //where TDelegate : Map<int, string>
        {
            Map<int, string> map = new Map<int, string>((i) => "");
            Map<int, string> map1 = (i) => "";
            Map<int, string> map2 = TestMap;
            Func<int, string> f1 = TestMap;

            //var mapped = @delegate.Map<int, string>(i => "");
        }
        public static string TestMap(int i)
        {
            return i.ToString();
        }
        public static void TestTestDelegate()
        {
            TestDelegate(new Func<int>(() => 2));
        }

    }
}
