using System;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Read-only message routes owned by a single bus instance.</summary>
public interface IMessageRouteTable
{
    /// <summary>Resolves the destination configured for a message contract.</summary>
    /// <typeparam name="T">The message contract type.</typeparam>
    /// <param name="destinationAddress">Receives the configured destination when a route resolves successfully; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a route resolves to a destination; otherwise, <see langword="false"/>.</returns>
    bool TryGetDestinationAddress<T>([NotNullWhen(true)] out Uri? destinationAddress)
        where T : class;

    /// <summary>Resolves the destination configured for a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="destinationAddress">Receives the configured destination when a route resolves successfully; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a route resolves to a destination; otherwise, <see langword="false"/>.</returns>
    bool TryGetDestinationAddress(Type messageType, [NotNullWhen(true)] out Uri? destinationAddress);
}
