using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Defines the contract for session id message send topology convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISessionIdMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Sets formatter.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    void SetFormatter(ISessionIdFormatter formatter);
    /// <summary>
    /// Sets formatter.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    void SetFormatter(IMessageSessionIdFormatter<TMessage> formatter);
}
