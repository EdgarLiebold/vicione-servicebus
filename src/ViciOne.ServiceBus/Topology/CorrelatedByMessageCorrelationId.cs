using System;

namespace ViciOne.ServiceBus.Topology;

/// <summary>
/// Provides a correlated by message correlation id implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class CorrelatedByMessageCorrelationId<T> :
    IMessageCorrelationId<T>
    where T : class, CorrelatedBy<Guid>
{
    /// <summary>
    /// Attempts to get correlation id.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetCorrelationId(T message, out Guid correlationId)
    {
        correlationId = message.CorrelationId;

        return correlationId != Guid.Empty;
    }
}
