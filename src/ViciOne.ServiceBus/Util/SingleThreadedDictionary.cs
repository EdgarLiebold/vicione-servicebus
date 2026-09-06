using System;
using System.Collections;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Util;

/// <summary>Stores single threaded values by key.</summary>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public class SingleThreadedDictionary<TKey, TValue> :
    IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    readonly IEqualityComparer<TKey> _comparer;
    readonly object _lock;
    IDictionary<TKey, TValue> _dictionary;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="comparer">The comparer.</param>
    public SingleThreadedDictionary(IEqualityComparer<TKey>? comparer = default)
    {
        _comparer = comparer ?? EqualityComparer<TKey>.Default;

        _lock = new object();

        _dictionary = new Dictionary<TKey, TValue>(_comparer);
    }

    /// <summary>Gets enumerator.</summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _dictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>Gets the count.</summary>
    public int Count => _dictionary.Count;

    /// <summary>Determines whether the current value contains key.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool ContainsKey(TKey key)
    {
        return _dictionary.ContainsKey(key);
    }

    /// <summary>Attempts to get value.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    /// <summary>Gets or sets the value at the specified index.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    public TValue this[TKey key] => _dictionary[key];

    /// <summary>Gets the keys.</summary>
    public IEnumerable<TKey> Keys => _dictionary.Keys;

    /// <summary>Gets the values.</summary>
    public IEnumerable<TValue> Values => _dictionary.Values;

    /// <summary>Removes every item from the current collection.</summary>
    public void Clear()
    {
        if (_dictionary.Count == 0)
            return;

        lock (_lock)
            _dictionary = new Dictionary<TKey, TValue>(_comparer);
    }

    /// <summary>Attempts to add.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="valueFactory">The value factory.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        lock (_lock)
        {
            if (_dictionary.ContainsKey(key))
                return false;

            var value = valueFactory(key);
            _dictionary = new Dictionary<TKey, TValue>(_dictionary, _comparer) { [key] = value };

            return true;
        }
    }

    /// <summary>Attempts to remove.</summary>
    /// <param name="key">The key used to identify the requested entry.</param>
    /// <param name="value">Receives the value produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryRemove(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        lock (_lock)
        {
            if (!_dictionary.TryGetValue(key, out value))
                return false;

            var replacement = new Dictionary<TKey, TValue>(_dictionary, _comparer);

            var result = replacement.Remove(key);

            _dictionary = replacement;

            return result;
        }
    }
}
