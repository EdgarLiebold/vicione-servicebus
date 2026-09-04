using Azure.Messaging.ServiceBus.Administration;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for receive settings.
/// </summary>
public interface ReceiveSettings :
    ClientSettings
{
    /// <summary>
    /// Gets create queue options.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    CreateQueueOptions GetCreateQueueOptions();
}
