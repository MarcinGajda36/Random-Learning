using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MarcinGajda.NewSwitches
{
    public static class NewSwitch
    {
        public static int? Test1(IList<int> list) => list switch
        {
            null => null,
            var xs when xs.Any() => xs.First(),
            _ => null,
        };
        public static string? Test2(IDictionary<int, string> dictionary, int key) => dictionary switch
        {
            null => null,
            var dict when dict.TryGetValue(key, out var val) => val,
            _ => null,
        };
    }
}
