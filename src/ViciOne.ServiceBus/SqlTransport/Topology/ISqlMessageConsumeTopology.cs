namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Defines the operations required by sql message consume topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISqlMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
