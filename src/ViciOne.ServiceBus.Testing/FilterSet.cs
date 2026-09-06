using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Stores a unique set of filter values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class FilterSet<T>
    where T : class
{
    readonly List<FilterDelegate<T>> _list;
    FilterDelegate<T> _all = x => true;
    FilterDelegate<T> _any = x => true;
    FilterDelegate<T> _notAny = x => false;

    /// <summary>Initializes a new instance.</summary>
    protected FilterSet()
    {
        _list = new List<FilterDelegate<T>>();
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The filter set produced by the operation.</returns>
    protected FilterSet<T> Add(FilterDelegate<T> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _all = x => _list.All(predicate => predicate(x));
        _any = x => _list.Any(predicate => predicate(x));
        _notAny = x => !_any(x);

        _list.Add(filter);

        return this;
    }

    /// <summary>Returns every available value.</summary>
    /// <param name="target">The target.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool All(T target)
    {
        return _all(target);
    }

    /// <summary>Selects any matching value.</summary>
    /// <param name="target">The target.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Any(T target)
    {
        return _any(target);
    }

    /// <summary>Determines whether no matching value exists.</summary>
    /// <param name="target">The target.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool NotAny(T target)
    {
        return _notAny(target);
    }

    /// <summary>Selects no values.</summary>
    /// <param name="target">The target.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool None(T target)
    {
        return _list.Count == 0 || !_any(target);
    }
}
