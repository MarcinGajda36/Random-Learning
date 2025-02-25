using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace MarcinGajda.Collections;

public class ObjectWithCollectionsProps
{
    private readonly int[] _arr = Array.Empty<int>();
    public ReadOnlyCollection<int> Arr1 => Array.AsReadOnly(_arr);

    private readonly List<int> _list = new List<int>();
    public ReadOnlyCollection<int> List => _list.AsReadOnly();

    private readonly ImmutableList<int> _immutableList = ImmutableList<int>.Empty;
    public ImmutableList<int> ImmutableList => _immutableList;
}
