using System.Collections.Generic;

namespace MarcinGajda.Collections;


internal delegate TResult ArgumentFunc<TElement, TArgument, out TResult>(in TElement element, in TArgument argument);

internal readonly record struct ByIndexEnumerator<TElement>(IReadOnlyList<TElement> Elements)
{
    internal void Select<TArgument, TResult>(in TArgument argument, ArgumentFunc<TElement, TArgument, TResult> func)
        => FirstIterator.Create(
            Elements,
            KeyValuePair.Create(argument, func),
            static (in TElement element, in KeyValuePair<TArgument, ArgumentFunc<TElement, TArgument, TResult>> arguments)
                => (true, arguments.Value(element, arguments.Key)));
}

internal static class FirstIterator
{
    internal static FirstIterator<
        TElement,
        KeyValuePair<TArgument, ArgumentFunc<TElement, TArgument, TResult>>,
        (bool, TResult)>
        Create<TElement, TArgument, TResult>(
        IReadOnlyList<TElement> elements,
        in KeyValuePair<TArgument, ArgumentFunc<TElement, TArgument, TResult>> argument,
        ArgumentFunc<TElement, KeyValuePair<TArgument, ArgumentFunc<TElement, TArgument, TResult>>, (bool, TResult)> func)
        => new(elements, argument, func);
}

internal readonly record struct FirstIterator<TElement, TArgument, TResult>(
    IReadOnlyList<TElement> Elements,
    in TArgument Argument,
    ArgumentFunc<TElement, TArgument, TResult> Func)
{

}
