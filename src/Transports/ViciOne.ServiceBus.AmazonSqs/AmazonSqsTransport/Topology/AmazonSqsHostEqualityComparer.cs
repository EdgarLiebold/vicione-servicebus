using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides an amazon sqs host equality comparer implementation.
/// </summary>
public sealed class AmazonSqsHostEqualityComparer :
    IEqualityComparer<AmazonSqsHostSettings>
{
    /// <summary>
    /// Gets the default value.
    /// </summary>
    public static IEqualityComparer<AmazonSqsHostSettings> Default { get; } = new AmazonSqsHostEqualityComparer();

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="x">The x value.</param>
    /// <param name="y">The y value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool Equals(AmazonSqsHostSettings? x, AmazonSqsHostSettings? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (ReferenceEquals(x, null))
            return false;

        if (ReferenceEquals(y, null))
            return false;

        return x.ScopeTopics == y.ScopeTopics && x.HostAddress.Equals(y.HostAddress);
    }

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <param name="obj">The obj value.</param>
    /// <returns>The result of the operation.</returns>
    public int GetHashCode(AmazonSqsHostSettings obj)
    {
        return HashCode.Combine(obj.HostAddress, obj.ScopeTopics);
    }
}
