namespace SpansAndStuff;
internal class InRef
{
    public static void TestInTest()
    {
        TestIn(5);
    }

    public static int TestIn(in int x)
    {
        return x;
    }

    public static void TestRefTest()
    {
        var five = 5;
        TestRef(ref five);
        //TestRef(5); // This doesn't compile
    }

    public static int TestRef(ref int x)
    {
        return x;
    }
}
