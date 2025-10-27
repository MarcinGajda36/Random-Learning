using System;
using System.Collections.Generic;

namespace MarcinGajda.Collections;

public readonly struct Iterator<TCollection, TElement>
    where TCollection : IEnumerable<TElement>
{
    private readonly TCollection _collection;

    public Iterator(TCollection collection) => _collection = collection;

}
public static class Iterator
{
    public static Iterator<TCollection, TElement> Create<TCollection, TElement>(TCollection collection)
        where TCollection : IEnumerable<TElement>
        => new(collection);

    public static Iterator<TCollection, TElement> Create<TCollection, TElement>(TCollection collection, Func<TElement, TElement> func)
        where TCollection : IEnumerable<TElement>
        => new(collection);
}

public static class Test12312312312
{
    public static void Test()
    {
        int[] arr = [1, 2, 3];
        var iterator = Iterator.Create(arr, (int x) => x);
    }
}