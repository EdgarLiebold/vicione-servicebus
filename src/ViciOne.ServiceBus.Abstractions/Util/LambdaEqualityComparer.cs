using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Util;

/// <summary>Compares lambda equality values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class LambdaEqualityComparer<T> :
    IEqualityComparer<T>
    where T : class
{
    readonly Func<T, T, bool> _comparer;
    readonly Func<T, int> _hash;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="comparer">The comparer.</param>
    public LambdaEqualityComparer(Func<T, T, bool> comparer)
        : this(comparer, o => 0)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="comparer">The comparer.</param>
    /// <param name="hash">The hash.</param>
    public LambdaEqualityComparer(Func<T, T, bool> comparer, Func<T?, int> hash)
    {
        if (comparer == null)
            throw new ArgumentNullException(nameof(comparer));
        if (hash == null)
            throw new ArgumentNullException(nameof(hash));

        _comparer = comparer;
        _hash = hash;
    }

    /// <summary>Determines whether this instance equals the supplied value.</summary>
    /// <param name="x">The <c>x</c> value.</param>
    /// <param name="y">The <c>y</c> value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(T? x, T? y)
    {
        if (x is null)
            return y is null;
        if (y is null)
            return false;

        return _comparer(x, y);
    }

    /// <summary>Gets hash code.</summary>
    /// <param name="obj">The obj.</param>
    /// <returns>The hash code for this instance.</returns>
    public int GetHashCode(T obj)
    {
        return _hash(obj);
    }
}
