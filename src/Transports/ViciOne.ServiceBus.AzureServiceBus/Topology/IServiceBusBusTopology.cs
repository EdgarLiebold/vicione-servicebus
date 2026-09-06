namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Exposes Azure Service Bus send and publish topology for a bus.</summary>
public interface IServiceBusBusTopology :
    IBusTopology
{
    /// <summary>Gets the bus-wide Azure Service Bus publish topology.</summary>
    new IServiceBusPublishTopology PublishTopology { get; }

    /// <summary>Gets the bus-wide Azure Service Bus send topology.</summary>
    new IServiceBusSendTopology SendTopology { get; }

    /// <summary>Gets the publish topology for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <returns>The message-specific Azure Service Bus publish topology.</returns>
    new IServiceBusMessagePublishTopology<T> Publish<T>()
        where T : class;

    /// <summary>Gets the send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <returns>The message-specific Azure Service Bus send topology.</returns>
    new IServiceBusMessageSendTopology<T> Send<T>()
        where T : class;
}
