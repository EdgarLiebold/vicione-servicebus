using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Compares ActiveMQ hosts by case-insensitive host name and port.</summary>
public sealed class ActiveMqHostEqualityComparer :
    IEqualityComparer<ActiveMqHostSettings>
{
    /// <summary>Gets the shared host comparer.</summary>
    public static IEqualityComparer<ActiveMqHostSettings> Default { get; } = new ActiveMqHostEqualityComparer();

    /// <summary>Determines whether two host settings address the same host name and port.</summary>
    /// <param name="x">The first host settings.</param>
    /// <param name="y">The second host settings.</param>
    /// <returns><see langword="true" /> when both settings address the same host and port; otherwise, <see langword="false" />.</returns>
    public bool Equals(ActiveMqHostSettings? x, ActiveMqHostSettings? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (ReferenceEquals(x, null))
            return false;

        if (ReferenceEquals(y, null))
            return false;

        return string.Equals(x.Host, y.Host, StringComparison.OrdinalIgnoreCase) && x.Port == y.Port;
    }

    /// <summary>Computes a hash code from a host name and port.</summary>
    /// <param name="obj">The host settings.</param>
    /// <returns>The host-and-port hash code.</returns>
    public int GetHashCode(ActiveMqHostSettings obj)
    {
        unchecked
        {
            var hashCode = obj.Host is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Host);
            hashCode = (hashCode * 397) ^ obj.Port;
            return hashCode;
        }
    }
}
