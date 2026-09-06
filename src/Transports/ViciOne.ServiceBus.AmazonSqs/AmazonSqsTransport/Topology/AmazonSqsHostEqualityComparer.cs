using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Compares Amazon SQS hosts by host address and topic-scoping behavior.</summary>
public sealed class AmazonSqsHostEqualityComparer :
    IEqualityComparer<AmazonSqsHostSettings>
{
    /// <summary>Gets the shared Amazon SQS host comparer.</summary>
    public static IEqualityComparer<AmazonSqsHostSettings> Default { get; } = new AmazonSqsHostEqualityComparer();

    /// <summary>Determines whether two host settings identify the same scoped host.</summary>
    /// <param name="x">The first host settings.</param>
    /// <param name="y">The second host settings.</param>
    /// <returns><see langword="true"/> when the host address and topic-scoping flag match; otherwise, <see langword="false"/>.</returns>
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

    /// <summary>Computes a hash code from the host address and topic-scoping flag.</summary>
    /// <param name="obj">The host settings.</param>
    /// <returns>The host identity hash code.</returns>
    public int GetHashCode(AmazonSqsHostSettings obj)
    {
        return HashCode.Combine(obj.HostAddress, obj.ScopeTopics);
    }
}
