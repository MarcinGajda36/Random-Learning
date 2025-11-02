using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HighPerf;

public class Fibonacci
{
    public static int Calculate(int number)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 0);
        if (number == 0)
            return 0;
        if (number <= 2)
            return 1;

        var previous = 0;
        var current = 1;
        while (--number > 0)
        {
            var oldCurrent = current;
            current += previous;
            previous = oldCurrent;
        }
        return current;
    }
}