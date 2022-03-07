using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarcinGajda.Collections;

public static class CollectionsExtentions
{
    public static Task<IReadOnlyCollection<Exception>> ForEachAsync<TElement>(
        this IEnumerable<TElement> elements,
        Func<TElement, Task> func)
        => ForEachAsync(elements, func, Environment.ProcessorCount);

    public static async Task<IReadOnlyCollection<Exception>> ForEachAsync<TElement>(
        this IEnumerable<TElement> elements,
        Func<TElement, Task> func,
        int maxDegreeOfParallelism)
    {
        var exceptions = new List<Exception>();
        static void AddException(List<Exception> exceptions, Task completed)
        {
            if (completed.IsFaulted)
            {
                exceptions.Add(completed.Exception!);
            }
        }

        var activeTasks = new LinkedList<Task>();
        foreach (var element in elements)
        {
            if (activeTasks.Count < maxDegreeOfParallelism)
            {
                activeTasks.AddLast(func(element));
            }
            else
            {
                var completed = await Task.WhenAny(activeTasks);
                AddException(exceptions, completed);
                activeTasks.Remove(completed);
                activeTasks.AddLast(func(element));
            }
        }

        while (activeTasks.Count > 0)
        {
            var completed = await Task.WhenAny(activeTasks);
            AddException(exceptions, completed);
            activeTasks.Remove(completed);
        }

        return exceptions;
    }

    public static void Test()
    {
        var many = Enumerable.Range(0, 10).Select(i => Enumerable.Range(0, i));
        var flatten =
            from collection in many
            from element in collection
            select element;

    }
}
