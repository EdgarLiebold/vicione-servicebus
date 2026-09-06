namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Describes Azure Service Bus send conventions for a message contract.</summary>
/// <typeparam name="TMessage">The sent message contract.</typeparam>
public interface IServiceBusMessageSendTopology<TMessage> :
    IMessageSendTopology<TMessage>
    where TMessage : class
{
}
