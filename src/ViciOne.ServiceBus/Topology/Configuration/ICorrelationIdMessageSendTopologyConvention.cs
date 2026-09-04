namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for correlation id message send topology convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ICorrelationIdMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Sets correlation id.
    /// </summary>
    /// <param name="messageCorrelationId">The message correlation id value.</param>
    void SetCorrelationId(IMessageCorrelationId<TMessage> messageCorrelationId);

    /// <summary>
    /// Tries to get the message correlation id
    /// </summary>
    /// <param name="messageCorrelationId"></param>
    /// <returns></returns>
    bool TryGetMessageCorrelationId([NotNullWhen(true)] out IMessageCorrelationId<TMessage>? messageCorrelationId);
}
