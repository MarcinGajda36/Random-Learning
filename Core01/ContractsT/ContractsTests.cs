using System;
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

public abstract class Equatable<T> : IEquatable<T>
{
    protected abstract bool EqualsCore(T other);

    public bool Equals(T? other)
    {
        // Compared to interfaces abstract class lets me do some pre and post validations and create guarantees across implementers 
        ArgumentNullException.ThrowIfNull(other);

        var core = EqualsCore(other);

        if (core == false && ReferenceEquals(this, other))
        {
            throw new Exception();
        }
        return core;
    }
}
