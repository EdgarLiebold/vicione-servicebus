using ViciOne.ServiceBus.AzureServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Resolves Azure Service Bus entity settings for send, error, and dead-letter destinations.</summary>
public interface IServiceBusSendTopology :
    ISendTopology
{
    /// <summary>Gets the send topology for a message contract.</summary>
    /// <typeparam name="T">The sent message contract.</typeparam>
    /// <returns>The message-specific send topology.</returns>
    new IServiceBusMessageSendTopology<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Builds send settings for an endpoint address.</summary>
    /// <param name="address">The queue or topic address.</param>
    /// <returns>The resolved entity and addressing settings.</returns>
    SendSettings GetSendSettings(ServiceBusEndpointAddress address);

    /// <summary>Builds settings for an endpoint's error queue.</summary>
    /// <param name="configurator">The source queue configuration.</param>
    /// <returns>The configured error-queue settings.</returns>
    SendSettings GetErrorSettings(IServiceBusQueueConfigurator configurator);
    /// <summary>Builds settings for an endpoint's skipped-message queue.</summary>
    /// <param name="configurator">The source queue configuration.</param>
    /// <returns>The configured skipped-message queue settings.</returns>
    SendSettings GetDeadLetterSettings(IServiceBusQueueConfigurator configurator);
}
