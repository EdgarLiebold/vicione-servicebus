using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Combines predicates used to match observed values.</summary>
/// <typeparam name="TContext">The observed context type.</typeparam>
public abstract class FilterSet<TContext>
    where TContext : class
{
    readonly List<FilterDelegate<TContext>> _list;
    FilterDelegate<TContext> _all = _ => true;
    FilterDelegate<TContext> _any = _ => true;
    FilterDelegate<TContext> _notAny = _ => false;

    /// <summary>Creates an empty filter set that accepts values by default.</summary>
    protected FilterSet()
    {
        _list = new List<FilterDelegate<TContext>>();
    }

    /// <summary>Adds a predicate to this set.</summary>
    /// <param name="filter">The predicate to add.</param>
    /// <returns>This filter set.</returns>
    protected FilterSet<TContext> Add(FilterDelegate<TContext> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        _all = x => _list.All(predicate => predicate(x));
        _any = x => _list.Any(predicate => predicate(x));
        _notAny = x => !_any(x);

        _list.Add(filter);

        return this;
    }

    /// <summary>Determines whether a value satisfies every configured predicate.</summary>
    /// <param name="target">The value to evaluate.</param>
    /// <returns><see langword="true"/> when every predicate matches, including when the set is empty; otherwise, <see langword="false"/>.</returns>
    public bool All(TContext target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return _all(target);
    }

    /// <summary>Determines whether a value satisfies at least one configured predicate.</summary>
    /// <param name="target">The value to evaluate.</param>
    /// <returns><see langword="true"/> when a predicate matches or the set is empty; otherwise, <see langword="false"/>.</returns>
    public bool Any(TContext target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return _any(target);
    }

    /// <summary>Determines whether a value satisfies none of the configured predicates.</summary>
    /// <param name="target">The value to evaluate.</param>
    /// <returns><see langword="true"/> when the set is nonempty and no predicate matches; otherwise, <see langword="false"/>.</returns>
    public bool NotAny(TContext target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return _notAny(target);
    }

    /// <summary>Determines whether a value is accepted by an empty set or satisfies none of its predicates.</summary>
    /// <param name="target">The value to evaluate.</param>
    /// <returns><see langword="true"/> when the set is empty or no predicate matches; otherwise, <see langword="false"/>.</returns>
    public bool None(TContext target)
    {
        ArgumentNullException.ThrowIfNull(target);
        return _list.Count == 0 || !_any(target);
    }
}
