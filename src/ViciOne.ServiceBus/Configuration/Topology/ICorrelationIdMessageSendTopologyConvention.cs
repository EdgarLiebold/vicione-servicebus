namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures correlation identifiers for one sent message contract.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
public interface ICorrelationIdMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>Sets the correlation resolver with precedence over inferred sources.</summary>
    /// <param name="messageCorrelationId">The correlation resolver.</param>
    void SetCorrelationId(IMessageCorrelationId<TMessage> messageCorrelationId);

    /// <summary>Attempts to get the configured or inferred correlation resolver.</summary>
    /// <param name="messageCorrelationId">Receives the correlation resolver when one is available.</param>
    /// <returns><see langword="true" /> when a resolver is available; otherwise, <see langword="false" />.</returns>
    bool TryGetCorrelationIdResolver([NotNullWhen(true)] out IMessageCorrelationId<TMessage>? messageCorrelationId);
}
