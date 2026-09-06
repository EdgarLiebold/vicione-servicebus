using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Describes an Azure Service Bus queue receive endpoint and its queue declaration.</summary>
public interface ReceiveSettings :
    ClientSettings
{
    /// <summary>Creates the Azure administration options used to declare the receive queue.</summary>
    /// <returns>The queue declaration options.</returns>
    CreateQueueOptions GetCreateQueueOptions();
}
