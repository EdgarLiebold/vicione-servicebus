using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Provides a lambda equality comparer implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class LambdaEqualityComparer<T> :
    IEqualityComparer<T>
    where T : class
{
    readonly Func<T, T, bool> _comparer;
    readonly Func<T, int> _hash;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="comparer">The comparer value.</param>
    public LambdaEqualityComparer(Func<T, T, bool> comparer)
        : this(comparer, o => 0)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="comparer">The comparer value.</param>
    /// <param name="hash">The hash value.</param>
    public LambdaEqualityComparer(Func<T, T, bool> comparer, Func<T?, int> hash)
    {
        if (comparer == null)
            throw new ArgumentNullException(nameof(comparer));
        if (hash == null)
            throw new ArgumentNullException(nameof(hash));

        _comparer = comparer;
        _hash = hash;
    }

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="x">The x value.</param>
    /// <param name="y">The y value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(T? x, T? y)
    {
        if (x == null || y == null)
            return false;

        return _comparer(x, y);
    }

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <param name="obj">The obj value.</param>
    /// <returns>The result of the operation.</returns>
    public int GetHashCode(T obj)
    {
        return _hash(obj);
    }
}
