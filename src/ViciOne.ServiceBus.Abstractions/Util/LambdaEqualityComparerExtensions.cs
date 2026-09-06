using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Util;

/// <summary>
/// Adds sequence operations that use delegate-based equality.
/// </summary>
public static class LambdaEqualityComparerExtensions
{
    /// <summary>
    /// Returns distinct elements using the supplied equality function.
    /// </summary>
    public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
        where T : class
    {
        return source.Distinct(new LambdaEqualityComparer<T>(comparer));
    }

    /// <summary>
    /// Returns the elements in the first sequence that are absent from the second sequence.
    /// </summary>
    public static IEnumerable<T> Except<T>(this IEnumerable<T> first, IEnumerable<T> second, Func<T?, T?, bool> comparer)
        where T : class
    {
        return first.Except(second, new LambdaEqualityComparer<T>(comparer));
    }
}
