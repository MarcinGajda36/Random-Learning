namespace MarcinGajda.GenericsTests;
using System;

// I want adder that concats positive number to the end of string, throws on null string or negative number

// For inheritance i can enforce that all implementation enforce contract.
public abstract class AdderBase
{
    public string Adder(string left, int positiveNumber)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentOutOfRangeException.ThrowIfLessThan(positiveNumber, 0);
        return AdderCore(left, positiveNumber);
    }

    protected abstract string AdderCore(string left, int positiveNumber);
}

// For interface there is no guarantee, only trust and expectation.
public interface IAdder
{
    string Adder(string left, int positiveNumber);
}
