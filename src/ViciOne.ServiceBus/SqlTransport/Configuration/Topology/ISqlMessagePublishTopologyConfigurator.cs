namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql message publish topology configurator.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISqlMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    ISqlMessagePublishTopology<TMessage>,
    ISqlMessagePublishTopologyConfigurator
    where TMessage : class
{
}


/// <summary>
/// Defines the contract for sql message publish topology configurator.
/// </summary>
public interface ISqlMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator,
    ISqlTopicConfigurator
{
}
