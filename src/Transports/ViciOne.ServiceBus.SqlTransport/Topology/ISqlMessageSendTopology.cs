namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Defines the operations required by sql message send topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISqlMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
