using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Read-only message routes owned by a single bus instance.</summary>
public interface IMessageRouteTable
{
    /// <summary>Attempts to get destination address.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="destinationAddress">Receives the destination address produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetDestinationAddress<T>(out Uri destinationAddress)
        where T : class;

    /// <summary>Attempts to get destination address.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="destinationAddress">Receives the destination address produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetDestinationAddress(Type messageType, out Uri destinationAddress);
}
