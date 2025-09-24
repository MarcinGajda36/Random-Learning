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

    public static bool IsPalindrome1(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return text switch
        {
            // Notes about pattern matching:
            // 1) I like going directly for the case i am interested in first, instead of excluding other cases first,
            // 2) I like making each case 'self contained', it contradicts 'solving a problems you don't have' a bit though
            // 3) I like assigning variable after pattern, it helps with using wrong variable in wrong place, especially with copy-paste
            { Length: > 1 } multiChar => Core(multiChar),
            { Length: <= 1 } => true,
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

    public static bool IsPalindrome2(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var leftToCheck = text.AsSpan();
        while (leftToCheck is [var first, .. var rest, var last])
        {
            if (first != last)
            {
                return false;
            }

            leftToCheck = rest;
        }

        return true;
    }
}
