using System;
using NUnit.Framework;
using HighPerf;

namespace TestProject1;

public class FibonacciTests
{
    [Test]
    public void Calculate_Negative_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Fibonacci.Calculate(-1));
    }

    [Test]
    public void Calculate_Zero_ReturnsZero()
    {
        var result = Fibonacci.Calculate(0);
        Assert.That(result, Is.Zero);
    }

    [Test]
    public void Calculate_OneAndTwo_ReturnOne()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(Fibonacci.Calculate(1), Is.EqualTo(1));
            Assert.That(Fibonacci.Calculate(2), Is.EqualTo(1));
        }
    }

    [Test]
    public void Calculate_SmallSequence_ReturnsExpected()
    {
        // Known sequence for this implementation: 0->1,1->1,2->1,3->2,4->3,5->5,6->8
        var expected = new[] { 0, 1, 1, 2, 3, 5, 8, 13 };
        for (var i = 0; i < expected.Length; ++i)
        {
            var actual = Fibonacci.Calculate(i);
            Assert.That(actual, Is.EqualTo(expected[i]), $"Fib({i})");
        }
    }
}
