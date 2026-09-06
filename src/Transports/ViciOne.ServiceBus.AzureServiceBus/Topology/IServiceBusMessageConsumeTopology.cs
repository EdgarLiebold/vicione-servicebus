namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Describes the Azure Service Bus consume topology for a message contract.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public interface IServiceBusMessageConsumeTopology<TMessage> :
    IMessageConsumeTopology<TMessage>
    where TMessage : class
{
}
