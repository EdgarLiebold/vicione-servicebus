using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>
/// Provides an active mq host equality comparer implementation.
/// </summary>
public sealed class ActiveMqHostEqualityComparer :
    IEqualityComparer<ActiveMqHostSettings>
{
    /// <summary>
    /// Gets the default value.
    /// </summary>
    public static IEqualityComparer<ActiveMqHostSettings> Default { get; } = new ActiveMqHostEqualityComparer();

    /// <summary>
    /// Determines whether this instance equals the supplied value.
    /// </summary>
    /// <param name="x">The x value.</param>
    /// <param name="y">The y value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Gets hash code.
    /// </summary>
    /// <param name="obj">The obj value.</param>
    /// <returns>The result of the operation.</returns>
    public int GetHashCode(ActiveMqHostSettings obj)
    {
        unchecked
        {
            var hashCode = obj.Host?.GetHashCode() ?? 0;
            hashCode = (hashCode * 397) ^ obj.Port;
            return hashCode;
        }
    }
}
