using System;

namespace ViciOne.ServiceBus.Topology;

/// <summary>
/// Provides a nullable delegate message correlation id implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class NullableDelegateMessageCorrelationId<T> :
    IMessageCorrelationId<T>
    where T : class
{
    readonly Func<T, Guid?> _getCorrelationId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="getCorrelationId">The get correlation id value.</param>
    public NullableDelegateMessageCorrelationId(Func<T, Guid?> getCorrelationId)
    {
        _getCorrelationId = getCorrelationId;
    }

    /// <summary>
    /// Attempts to get correlation id.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetCorrelationId(T message, out Guid correlationId)
    {
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
