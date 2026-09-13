using System.Collections.Generic;

namespace ViciOne.ServiceBus.Analyzers.Internals;

sealed class SymbolIndex<T>(int capacity, IEqualityComparer<T>? comparer)
    where T : notnull
{
    readonly Dictionary<T, int> _nodes = new(capacity, comparer);

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
