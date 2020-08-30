using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;

namespace MarcinGajda.ContractsT
{
    public class ContractsTests
    {
        public static int Test(int value)
        {
            Contract.Requires(value > 0, "value can't be let then 0");
            Contract.Requires(value < 0, "value can't be let then 0");
            return value;
        }
    }
}
