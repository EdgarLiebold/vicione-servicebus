namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a set correlation id selector implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SetCorrelationIdSelector<T> :
    ICorrelationIdSelector<T>
    where T : class
{
    readonly IMessageCorrelationId<T> _messageCorrelationId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="messageCorrelationId">The message correlation id value.</param>
    public SetCorrelationIdSelector(IMessageCorrelationId<T> messageCorrelationId)
    {
        _messageCorrelationId = messageCorrelationId;
    }

    /// <summary>
    /// Attempts to get set correlation id.
    /// </summary>
    /// <param name="messageCorrelationId">The message correlation id value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetSetCorrelationId([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId)
    {
        messageCorrelationId = _messageCorrelationId;
        return true;
    }
}
