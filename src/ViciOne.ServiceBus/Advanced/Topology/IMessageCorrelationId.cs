using System;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Resolves an optional correlation identifier from a message contract.</summary>
/// <typeparam name="T">The message contract type.</typeparam>
public interface IMessageCorrelationId<in T>
    where T : class
{
    /// <summary>Attempts to resolve the message correlation identifier.</summary>
    /// <param name="message">The message whose identifier is requested.</param>
    /// <param name="correlationId">Receives the identifier when one is available.</param>
    /// <returns><see langword="true" /> when an identifier is available; otherwise, <see langword="false" />.</returns>
    bool TryGetCorrelationId(T message, out Guid correlationId);
}
