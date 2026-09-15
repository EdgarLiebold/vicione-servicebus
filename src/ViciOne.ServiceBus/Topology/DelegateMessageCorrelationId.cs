using System;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Reads a required correlation identifier from a message by using a caller-supplied selector.</summary>
/// <typeparam name="T">The message contract.</typeparam>
public sealed class DelegateMessageCorrelationId<T> :
    IMessageCorrelationId<T>
    where T : class
{
    readonly Func<T, Guid> _getCorrelationId;

    /// <summary>Creates a resolver backed by the specified selector.</summary>
    /// <param name="getCorrelationId">The selector that reads the identifier from a message.</param>
    public DelegateMessageCorrelationId(Func<T, Guid> getCorrelationId)
    {
        _getCorrelationId = getCorrelationId ?? throw new ArgumentNullException(nameof(getCorrelationId));
    }

    /// <summary>Reads the identifier and reports whether it is non-empty.</summary>
    /// <param name="message">The message whose identifier is read.</param>
    /// <param name="correlationId">The selected identifier.</param>
    /// <returns><see langword="true" /> when the selected identifier is non-empty; otherwise, <see langword="false" />.</returns>
    public bool TryGetCorrelationId(T message, out Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(message);

        correlationId = _getCorrelationId(message);

        return correlationId != Guid.Empty;
    }
}
