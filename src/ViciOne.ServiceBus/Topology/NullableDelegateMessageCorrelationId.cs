using System;

namespace ViciOne.ServiceBus.Topology;

/// <summary>Reads an optional correlation identifier from a message by using a caller-supplied selector.</summary>
/// <typeparam name="T">The message contract.</typeparam>
public sealed class NullableDelegateMessageCorrelationId<T> :
    IMessageCorrelationId<T>
    where T : class
{
    readonly Func<T, Guid?> _getCorrelationId;

    /// <summary>Creates a resolver backed by the specified selector.</summary>
    /// <param name="getCorrelationId">The selector that reads the optional identifier from a message.</param>
    public NullableDelegateMessageCorrelationId(Func<T, Guid?> getCorrelationId)
    {
        _getCorrelationId = getCorrelationId ?? throw new ArgumentNullException(nameof(getCorrelationId));
    }

    /// <summary>Reads the identifier and reports whether it has a non-empty value.</summary>
    /// <param name="message">The message whose identifier is read.</param>
    /// <param name="correlationId">The selected identifier, or <see cref="Guid.Empty" /> when absent.</param>
    /// <returns><see langword="true" /> when the selected identifier has a non-empty value; otherwise, <see langword="false" />.</returns>
    public bool TryGetCorrelationId(T message, out Guid correlationId)
    {
        ArgumentNullException.ThrowIfNull(message);

        Guid? id = _getCorrelationId(message);
        if (id.HasValue && id.Value != Guid.Empty)
        {
            correlationId = id.Value;
            return true;
        }

        correlationId = Guid.Empty;
        return false;
    }
}
