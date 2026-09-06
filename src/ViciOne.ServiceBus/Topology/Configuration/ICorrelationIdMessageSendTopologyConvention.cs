namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by correlation id message send topology convention.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ICorrelationIdMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>Sets correlation id.</summary>
    /// <param name="messageCorrelationId">The message correlation id.</param>
    void SetCorrelationId(IMessageCorrelationId<TMessage> messageCorrelationId);

    /// <summary>Tries to get the message correlation id.</summary>
    /// <param name="messageCorrelationId">Receives the message correlation id produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetMessageCorrelationId([NotNullWhen(true)] out IMessageCorrelationId<TMessage>? messageCorrelationId);
}
