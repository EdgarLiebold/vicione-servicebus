using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Provides a filter set implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class FilterSet<T>
    where T : class
{
    readonly List<FilterDelegate<T>> _list;
    FilterDelegate<T> _all = x => true;
    FilterDelegate<T> _any = x => true;
    FilterDelegate<T> _notAny = x => false;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    protected FilterSet()
    {
        _list = new List<FilterDelegate<T>>();
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    /// <returns>The result of the operation.</returns>
    protected FilterSet<T> Add(FilterDelegate<T> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _all = x => _list.All(predicate => predicate(x));
        _any = x => _list.Any(predicate => predicate(x));
        _notAny = x => !_any(x);

        _list.Add(filter);

        return this;
    }

    /// <summary>
    /// Performs the all operation.
    /// </summary>
    /// <param name="target">The target value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool All(T target)
    {
        return _all(target);
    }

    /// <summary>
    /// Performs the any operation.
    /// </summary>
    /// <param name="target">The target value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Any(T target)
    {
        return _any(target);
    }

    /// <summary>
    /// Performs the not any operation.
    /// </summary>
    /// <param name="target">The target value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool NotAny(T target)
    {
        return _notAny(target);
    }

    /// <summary>
    /// Performs the none operation.
    /// </summary>
    /// <param name="target">The target value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool None(T target)
    {
        return _list.Count == 0 || !_any(target);
    }
}
