using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Evaluates the predicate for composite.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class CompositePredicate<T>
{
    readonly List<Func<T, bool>> _list = new List<Func<T, bool>>();
    Func<T, bool> _matchesAll = x => true;
    Func<T, bool> _matchesAny = x => true;
    Func<T, bool> _matchesNone = x => false;

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="filter">The filter to add to the pipeline.</param>
    public void Add(Func<T, bool> filter)
    {
        _matchesAll = x => _list.All(predicate => predicate(x));
        _matchesAny = x => _list.Any(predicate => predicate(x));
        _matchesNone = x => !MatchesAny(x);

        _list.Add(filter);
    }

    /// <summary>Applies the <c>+</c> operator.</summary>
    /// <param name="invokes">The invokes.</param>
    /// <param name="filter">The filter to add to the pipeline.</param>
    /// <returns>The value produced by the operation.</returns>
    public static CompositePredicate<T> operator +(CompositePredicate<T> invokes, Func<T, bool> filter)
    {
        invokes.Add(filter);
        return invokes;
    }

    /// <summary>Determines whether the value matches all.</summary>
    /// <param name="target">The target.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool MatchesAll(T target)
    {
        return _matchesAll(target);
    }

    /// <summary>Determines whether the value matches any.</summary>
    /// <param name="target">The target.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool MatchesAny(T target)
    {
        return _matchesAny(target);
    }

    /// <summary>Determines whether the value matches none.</summary>
    /// <param name="target">The target.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool MatchesNone(T target)
    {
        return _matchesNone(target);
    }

    /// <summary>Determines whether none of the predicates match the target.</summary>
    /// <param name="target">The target.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool DoesNotMatchAny(T target)
    {
        return _list.Count == 0 || !MatchesAny(target);
    }
}
