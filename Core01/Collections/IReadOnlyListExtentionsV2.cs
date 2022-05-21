using System.Collections.Generic;

namespace MarcinGajda.Collections;


internal delegate TResult ArgumentFunc<TElement, TArgument, out TResult>(in TElement element, in TArgument argument);
internal delegate (bool, TResult) ArgumentBoolFunc<TElement, TArgument, TResult>(in TElement element, in TArgument argument);

internal readonly record struct ByIndexEnumerator<TElement>(IReadOnlyList<TElement> Elements)
{
    internal FirstIterator<TElement, KeyValuePair<TArgument, ArgumentFunc<TElement, TArgument, TResult>>, TResult> Select<TArgument, TResult>(in TArgument argument, ArgumentFunc<TElement, TArgument, TResult> func)
        => ByIndexEnumerator.CreateFirstIterator(
            Elements,
            KeyValuePair.Create(argument, func),
            static (in TElement element, in KeyValuePair<TArgument, ArgumentFunc<TElement, TArgument, TResult>> arguments)
            => (true, arguments.Value(element, arguments.Key)));
}

internal static class ByIndexEnumerator
{
    internal static ByIndexEnumerator<TElement> CreateByIndexEnumerator<TElement>(this IReadOnlyList<TElement> elements)
        => new(elements);

    internal static FirstIterator<
        TElement,
        KeyValuePair<TArgument, ArgumentFunc<TElement, TArgument, TResult>>,
        TResult>
        CreateFirstIterator<TElement, TArgument, TResult>(
        IReadOnlyList<TElement> elements,
        in KeyValuePair<TArgument, ArgumentFunc<TElement, TArgument, TResult>> argument,
        ArgumentBoolFunc<TElement, KeyValuePair<TArgument, ArgumentFunc<TElement, TArgument, TResult>>, TResult> func)
        => new(elements, argument, func);
}

internal readonly record struct FirstIterator<TElement, TArgument, TResult>(
    IReadOnlyList<TElement> Elements,
    in TArgument Argument,
    ArgumentBoolFunc<TElement, TArgument, TResult> Func)
{
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

internal static class Testtt
{
    internal static void Testttasdasd()
    {
        var arr = new int[] { 1, 2, 3 };
        arr
            .CreateByIndexEnumerator()
            .Select("", (in int x, in string y) => x);
    }
}