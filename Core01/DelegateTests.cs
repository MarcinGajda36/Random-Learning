using System;

namespace MarcinGajda
{
    public delegate TestStruct Testtt();
    public delegate TR Test10<in T1, in T2, out TR>(T1 t1, T2 t2);
    public static class DelegateTests
    {
        public static void TestFunc(Func<TestStruct> func)
        {
            var a = func();
        }
        public static void TestDel10(Test10<int, int, string> testtt)
        {
            var aasd = testtt(1, 2);
        }
        public static void TestDel(Testtt testtt)
        {
            var a = testtt();
        }
        public static void TestTest()
        {
            TestDel(() => new TestStruct());
            TestFunc(() => new TestStruct());
            TestDel10((i1, i2) => "");
        }
    }
    public struct TestStruct
    {
    }
}
