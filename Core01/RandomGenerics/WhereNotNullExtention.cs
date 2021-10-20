using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarcinGajda.RandomGenerics
{
    public static class WhereNotNullExtention
    {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        public static IEnumerable<T> WhereNotNull<T>(IEnumerable<T?> ts) => ts.Where(t => t != null);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
    }
}
