using System;
using System.Collections.Generic;

namespace MarcinGajda.Collections;


internal readonly record struct ReadOnlyListEnumerator<TElement>(IReadOnlyList<TElement> Elements)
{
    public ReadOnlyListEnumerator1<(TArgument, Func<TElement, TArgument, TResult>), TElement, TResult> Select<TArgument, TResult>(
        TArgument argument,
        Func<TElement, TArgument, TResult> Func)
        => new(
            Elements,
            (argument, Func),
            static (element, arguments) => (true, arguments.Item2(element, arguments.Item1)));

    public ReadOnlyListEnumerator1<(TArgument, Func<TElement, TArgument, bool>), TElement, TElement> Where<TArgument>(
        TArgument argument,
        Func<TElement, TArgument, bool> Func)
        => new(
            Elements,
            (argument, Func),
            static (element, argumens) => (argumens.Item2(element, argumens.Item1), element));
}

internal readonly record struct ReadOnlyListEnumerator1<TArgument, TElement, TResult>(
    IReadOnlyList<TElement> Elements,
    TArgument Argument,
    Func<TElement, TArgument, (bool, TResult)> Func)
{
    public ReadOnlyListEnumerator2<TArgument, TElement, TResult, (TArgument2, Func<TResult, TArgument2, TResult2>), TResult2> Select
        <TArgument2, TResult2>(TArgument2 argument2, Func<TResult, TArgument2, TResult2> func2)
        => SReadOnlyListEnumerator2.Create(
            this,
            (argument2, func2),
            static (element2, arguments2) => (true, arguments2.func2(element2, arguments2.argument2)));

    public ReadOnlyListEnumerator2<TArgument, TElement, TResult, (TArgument2, Func<TResult, TArgument2, bool>), TResult> Where
        <TArgument2, TResult2>(TArgument2 argument2, Func<TResult, TArgument2, bool> func2)
        => SReadOnlyListEnumerator2.Create(
            this,
            (argument2, func2),
            static (element2, arguments2) => (arguments2.func2(element2, arguments2.argument2), element2));

    public void Execute()
    {
        for (int index = 0; index < Elements.Count; index++)
        {
            Func(Elements[index], Argument);
        }
    }

    public List<TResult> ToList()
    {
        var results = new List<TResult>(Elements.Count);
        for (int index = 0; index < Elements.Count; index++)
        {
            var (succes, result) = Func(Elements[index], Argument);
            if (succes)
            {
                results.Add(result);
            }
        }
        return results;
    }
}

internal static class SReadOnlyListEnumerator2
{
    public static ReadOnlyListEnumerator2<TArgument1, TElement1, TResult1, TArgument2, TResult2> Create
        <TArgument1, TElement1, TResult1, TArgument2, TResult2>(
        ReadOnlyListEnumerator1<TArgument1, TElement1, TResult1> Enumerator1,
        TArgument2 Argument,
        Func<TResult1, TArgument2, (bool, TResult2)> Func)
        => new(Enumerator1, Argument, Func);
}

internal readonly record struct ReadOnlyListEnumerator2<TArgument1, TElement1, TResult1, TArgument2, TResult2>(
    ReadOnlyListEnumerator1<TArgument1, TElement1, TResult1> Enumerator1,
    TArgument2 Argument,
    Func<TResult1, TArgument2, (bool, TResult2)> Func)
{
    public void Execute()
    {
        var (elements, argument1, func1) = Enumerator1;
        for (int index = 0; index < elements.Count; index++)
        {
            var (success, result1) = func1(elements[index], argument1);
            if (success)
            {
                Func(result1, Argument);
            }
        }
    }

    public List<TResult2> ToList()
    {
        var (elements, argument1, func1) = Enumerator1;
        var results = new List<TResult2>(elements.Count);
        for (int index = 0; index < elements.Count; index++)
        {
            var (success1, result1) = func1(elements[index], argument1);
            if (success1)
            {
                var (success2, result2) = Func(result1, Argument);
                if (success2)
                {
                    results.Add(result2);
                }
            }
        }
        return results;
    }
}


internal static class IReadOnlyListExtentions
{
    public static ReadOnlyListEnumerator<TElement> CreateEnumerator<TElement>(this IReadOnlyList<TElement> elements)
        => new(elements);
}

internal static class Test
{
    public static void Test1()
    {
        var arr = new int[] { 1, 2, 3 };
        var results = arr
            .CreateEnumerator()
            .Select(0, (element, argument) => element + argument)
            .Select("", (element, argument) => argument)
            .ToList();

    }
}