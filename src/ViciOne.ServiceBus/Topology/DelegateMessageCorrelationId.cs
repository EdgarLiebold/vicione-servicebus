using System;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Represents the identifier for delegate message correlation.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class DelegateMessageCorrelationId<T> :
    IMessageCorrelationId<T>
    where T : class
{
    readonly Func<T, Guid> _getCorrelationId;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="getCorrelationId">The get correlation id.</param>
    public DelegateMessageCorrelationId(Func<T, Guid> getCorrelationId)
    {
        _getCorrelationId = getCorrelationId;
    }

    /// <summary>Attempts to get correlation id.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="correlationId">Receives the correlation id produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetCorrelationId(T message, out Guid correlationId)
    {
        correlationId = _getCorrelationId(message);

        return correlationId != Guid.Empty;
    }
}
