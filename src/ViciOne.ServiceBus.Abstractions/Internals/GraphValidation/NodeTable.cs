using System.Collections.Generic;

namespace ViciOne.ServiceBus.Internals.GraphValidation;

/// <summary>
/// Maintains an index of nodes so that regular ints can be used to execute algorithms
/// against objects with int-compare speed vs. .Equals() speed.
/// </summary>
/// <typeparam name="T">The value type.</typeparam>
internal class NodeTable<T>
    where T : notnull
{
    readonly IDictionary<T, int> _nodes;
    int _count;

    public NodeTable(int capacity)
    {
        _nodes = new Dictionary<T, int>(capacity);
    }

    /// <summary>
    /// Returns the index for the specified key, which can be any type that supports
    /// equality comparison.
    /// </summary>
    /// <param name="key">The key to retrieve.</param>
    public int this[T key]
    {
        get
        {
            if (_nodes.TryGetValue(key, out var value))
                return value;

            value = ++_count;
            _nodes.Add(key, value);

            return value;
        }
    }
}
