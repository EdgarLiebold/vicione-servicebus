using System;
using System.Collections;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides a single threaded dictionary implementation.
/// </summary>
/// <typeparam name="TKey">The t key type.</typeparam>
/// <typeparam name="TValue">The t value type.</typeparam>
public class SingleThreadedDictionary<TKey, TValue> :
    IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    readonly IEqualityComparer<TKey> _comparer;
    readonly object _lock;
    IDictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="comparer">The comparer value.</param>
    public SingleThreadedDictionary(IEqualityComparer<TKey>? comparer = default)
    {
        _comparer = comparer ?? EqualityComparer<TKey>.Default;

        _lock = new object();

        _dictionary = new Dictionary<TKey, TValue>(_comparer);
    }

    /// <summary>
    /// Gets enumerator.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return _dictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _dictionary.Count;

    /// <summary>
    /// Performs the contains key operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool ContainsKey(TKey key)
    {
        return _dictionary.ContainsKey(key);
    }

    /// <summary>
    /// Attempts to get value.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="key">The key value.</param>
    public TValue this[TKey key] => _dictionary[key];

    /// <summary>
    /// Gets the keys value.
    /// </summary>
    public IEnumerable<TKey> Keys => _dictionary.Keys;

    /// <summary>
    /// Gets the values value.
    /// </summary>
    public IEnumerable<TValue> Values => _dictionary.Values;

    /// <summary>
    /// Performs the clear operation.
    /// </summary>
    public void Clear()
    {
        if (_dictionary.Count == 0)
            return;

        lock (_lock)
            _dictionary = new Dictionary<TKey, TValue>(_comparer);
    }

    /// <summary>
    /// Performs the try add operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="valueFactory">The value factory value.</param>
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

    /// <summary>
    /// Performs the try remove operation.
    /// </summary>
    /// <param name="key">The key value.</param>
    /// <param name="value">The value.</param>
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
