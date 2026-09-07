using System;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Represents the identifier for correlated by message correlation.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class CorrelatedByMessageCorrelationId<T> :
    IMessageCorrelationId<T>
    where T : class, CorrelatedBy<Guid>
{
    /// <summary>Attempts to get correlation id.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="correlationId">Receives the correlation id produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetCorrelationId(T message, out Guid correlationId)
    {
        correlationId = message.CorrelationId;

        return correlationId != Guid.Empty;
    }
}
