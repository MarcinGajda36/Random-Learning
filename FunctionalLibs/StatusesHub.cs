using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;

namespace FunctionalLibs
{

    public class StatusesHub
    {
        static readonly Atom<HashMap<string, int>> atom = Atom(HashMap<string, int>());
        async Task StatusesHub1()
        {
            atom.Swap(hmap => hmap);

            var ps = HashMap(("a", 1), ("b", 2));
            var at = Atom(ps);
            at.Swap(current => current.TryAdd("c", 3));
            OptionAsync<int> oa = SomeAsync(Task.FromResult(1));
            oa.Map(i => i.ToString());
        }

    }
}
