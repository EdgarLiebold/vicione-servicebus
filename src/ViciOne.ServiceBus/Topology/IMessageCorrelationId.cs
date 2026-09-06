using System;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Defines the operations required by message correlation id.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IMessageCorrelationId<in T>
    where T : class
{
    /// <summary>Get the CorrelationId from the message, if available.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="correlationId">Receives the correlation id produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetCorrelationId(T message, out Guid correlationId);
}
