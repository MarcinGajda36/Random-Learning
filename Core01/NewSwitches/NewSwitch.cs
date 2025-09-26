using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

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
        // Argument checking notes:
        // 1) I am thinking about ArgumentNullException.ThrowIfNull in public methods and Debug.Assert in private methods
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

    public static TDestination[] ConvertAll<TSource, TDestination>(
        IEnumerable<TSource> source,
        Func<TSource, TDestination> mapper)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mapper);
        return source switch
        {
            TSource[] array => array switch
            {
                [] => [],
                var many => MapSpan(array, mapper),
            },
            List<TSource> list => list switch
            {
                [] => [],
                var many => MapSpan(CollectionsMarshal.AsSpan(many), mapper),
            },
            IReadOnlyCollection<TSource> collection => collection switch
            {
                { Count: < 1 } => [],
                { Count: >= 1 } notEmpty => MapCollection(notEmpty, mapper)
            },
            var enumerable => MapEnumerable(enumerable, mapper)
        };

        static TDestination[] MapSpan(Span<TSource> sources, Func<TSource, TDestination> mapper)
        {
            var destination = new TDestination[sources.Length];
            for (var index = 0; index < sources.Length; index++)
            {
                destination[index] = mapper(sources[index]);
            }
            return destination;
        }

        static TDestination[] MapCollection(IReadOnlyCollection<TSource> sources, Func<TSource, TDestination> mapper)
        {
            var destination = new TDestination[sources.Count];
            var index = 0;
            foreach (var source in sources)
            {
                destination[index++] = mapper(source);
            }
            return destination;
        }

        static TDestination[] MapEnumerable(IEnumerable<TSource> sources, Func<TSource, TDestination> mapper)
        {
            var destination = ImmutableArray.CreateBuilder<TDestination>();
            foreach (var source in sources)
            {
                destination.Add(mapper(source));
            }
            return destination.ToArray();
        }
    }
}
