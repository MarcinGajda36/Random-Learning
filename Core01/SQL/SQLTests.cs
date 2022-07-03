using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Runtime.ExceptionServices;

namespace MarcinGajda.SQL
{
    internal class SQLTests
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
