using System;

namespace MarcinGajda.Synchronizers.Pooling;
public class Pool<TKey, TPoolInstance>
{
    public TResult Use<TKey, TPoolInstance, TResult>(TKey key, Func<TKey, TPoolInstance, TResult> use)
        => throw null;
}
