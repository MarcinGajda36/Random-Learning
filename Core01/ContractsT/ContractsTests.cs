using System.Diagnostics.Contracts;

namespace MarcinGajda.ContractsT;


public interface ITest<T>
{
    public T Value { get; }
}

public class Test : ITest<int>, ITest<string>
{
    public int Value => throw new System.NotImplementedException();

    string ITest<string>.Value => throw new System.NotImplementedException();
}

public class ContractsTests
{

    public static int Test(int value)
    {
        Contract.Requires(value > 0, "value can't be let then 0");
        Contract.Requires(value < 0, "value can't be let then 0");
        return value;
    }
}
