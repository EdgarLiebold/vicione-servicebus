using System.Collections.Generic;

namespace ViciOne.ServiceBus.Analyzers.Internals;

sealed class ConversionGraph<T>
    where T : notnull
{
    readonly IList<HashSet<int>> _nodes;
    readonly SymbolIndex<T> _symbolIndex;

    public ConversionGraph(int capacity, IEqualityComparer<T>? comparer = null)
    {
        _nodes = new List<HashSet<int>>(capacity);
        _symbolIndex = new SymbolIndex<T>(capacity, comparer);
    }

    public void Add(T key, params T[] values)
    {
        HashSet<int> hashSet = _nodes[Index(key) - 1];
        for (var i = 0; i < values.Length; i++)
            hashSet.Add(Index(values[i]));
    }

    public bool Contains(T key, T value)
    {
        return _nodes[Index(key) - 1].Contains(Index(value));
    }

    int Index(T key)
    {
        var index = _symbolIndex[key];

        if (index <= _nodes.Count)
            return index;

        _nodes.Add(new HashSet<int>());
        return index;
    }
}
