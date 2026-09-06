namespace ViciOne.ServiceBus.Configuration;

/// <summary>Selects set correlation id values.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class SetCorrelationIdSelector<T> :
    ICorrelationIdSelector<T>
    where T : class
{
    readonly IMessageCorrelationId<T> _messageCorrelationId;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="messageCorrelationId">The message correlation id.</param>
    public SetCorrelationIdSelector(IMessageCorrelationId<T> messageCorrelationId)
    {
        _messageCorrelationId = messageCorrelationId;
    }

    /// <summary>Attempts to get set correlation id.</summary>
    /// <param name="messageCorrelationId">Receives the message correlation id produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetSetCorrelationId([NotNullWhen(true)] out IMessageCorrelationId<T>? messageCorrelationId)
    {
        messageCorrelationId = _messageCorrelationId;
        return true;
    }
}
