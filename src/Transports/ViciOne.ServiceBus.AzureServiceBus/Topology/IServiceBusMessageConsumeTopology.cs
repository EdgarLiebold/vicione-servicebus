namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus message consume topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IServiceBusMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
