using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.Data.SqlClient;

namespace MarcinGajda.SQL
{
    class SQLTests
    {
        public static void Test()
        {
            var sqlParameter = new SqlParameter()
            {
                ParameterName = @"@Param",
                SqlDbType = SqlDbType.Text,
                Direction = ParameterDirection.Input,
                Value = "test"
            };
            ExceptionDispatchInfo.Capture(new Exception()).Throw();
        }
    }
}
