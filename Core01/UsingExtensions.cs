using System;
using System.Collections.Generic;
using System.Text;

namespace MarcinGajda
{
    public static class UsingExtensions
    {
        public static TResult Using<TDisposable, TResult>(this TDisposable disposable, Func<TDisposable, TResult> func) where TDisposable : IDisposable
        {
            using (var disp = disposable) return func(disp);
        }
    }
}
