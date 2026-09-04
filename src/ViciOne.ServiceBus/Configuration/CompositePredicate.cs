using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a composite predicate implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class CompositePredicate<T>
{
    readonly List<Func<T, bool>> _list = new List<Func<T, bool>>();
    Func<T, bool> _matchesAll = x => true;
    Func<T, bool> _matchesAny = x => true;
    Func<T, bool> _matchesNone = x => false;

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="filter">The filter value.</param>
    public void Add(Func<T, bool> filter)
    {
        _matchesAll = x => _list.All(predicate => predicate(x));
        _matchesAny = x => _list.Any(predicate => predicate(x));
        _matchesNone = x => !MatchesAny(x);

        _list.Add(filter);
    }

    /// <summary>
    /// Applies the <c>+</c> operator.
    /// </summary>
    /// <param name="invokes">The invokes value.</param>
    /// <param name="filter">The filter value.</param>
    /// <returns>The result of the operation.</returns>
    public static CompositePredicate<T> operator +(CompositePredicate<T> invokes, Func<T, bool> filter)
    {
        invokes.Add(filter);
        return invokes;
    }

    /// <summary>
    /// Performs the matches all operation.
    /// </summary>
    /// <param name="target">The target value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool MatchesAll(T target)
    {
        return _matchesAll(target);
    }

    /// <summary>
    /// Performs the matches any operation.
    /// </summary>
    /// <param name="target">The target value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool MatchesAny(T target)
    {
        return _matchesAny(target);
    }

    /// <summary>
    /// Performs the matches none operation.
    /// </summary>
    /// <param name="target">The target value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool MatchesNone(T target)
    {
        return _matchesNone(target);
    }

    /// <summary>
    /// Performs the does not matche any operation.
    /// </summary>
    /// <param name="target">The target value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool DoesNotMatcheAny(T target)
    {
        return _list.Count == 0 || !MatchesAny(target);
    }
}
