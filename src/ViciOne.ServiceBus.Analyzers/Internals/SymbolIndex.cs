using System.Collections.Generic;

namespace ViciOne.ServiceBus.Analyzers.Internals;

sealed class SymbolIndex<T>
    where T : notnull
{
    readonly Dictionary<T, int> _nodes;

    public SymbolIndex(int capacity, IEqualityComparer<T>? comparer)
    {
        _nodes = new Dictionary<T, int>(capacity, comparer);
    }

    public int this[T key]
    {
        get
        {
            if (_nodes.TryGetValue(key, out var index))
                return index;

            index = _nodes.Count + 1;
            _nodes.Add(key, index);
            return index;
        }
    }
}
