namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures sql message publish topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISqlMessagePublishTopologyConfigurator<TMessage> :
    IMessagePublishTopologyConfigurator<TMessage>,
    ISqlMessagePublishTopology<TMessage>,
    ISqlMessagePublishTopologyConfigurator
    where TMessage : class
{
}


/// <summary>Configures sql message publish topology.</summary>
public interface ISqlMessagePublishTopologyConfigurator :
    IMessagePublishTopologyConfigurator
{
}
