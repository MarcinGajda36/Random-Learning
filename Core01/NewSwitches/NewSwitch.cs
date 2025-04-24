using System;
using System.Collections.Generic;
using System.Linq;

namespace MarcinGajda.NewSwitches;

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

    public static bool IsPalindrome(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text.AsSpan() switch
        {
        [] or [_] => true,
            var multiChar => Core(multiChar),
        };

        static bool Core(ReadOnlySpan<char> text)
        {
            for (var i = 0; i < text.Length / 2; ++i)
            {
                if (text[i] != text[^(1 + i)])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
