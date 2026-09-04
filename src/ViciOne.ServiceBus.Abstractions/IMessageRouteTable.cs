using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Read-only message routes owned by a single bus instance.
/// </summary>
public interface IMessageRouteTable
{
    /// <summary>
    /// Attempts to get destination address.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetDestinationAddress<T>(out Uri destinationAddress)
        where T : class;

    /// <summary>
    /// Attempts to get destination address.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetDestinationAddress(Type messageType, out Uri destinationAddress);
}
