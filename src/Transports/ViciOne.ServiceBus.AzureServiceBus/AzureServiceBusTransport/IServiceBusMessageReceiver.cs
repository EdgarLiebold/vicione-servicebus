using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Dispatches an Azure Service Bus delivery into a receive pipeline.</summary>
public interface IServiceBusMessageReceiver
{
    /// <summary>Processes a received Azure Service Bus message.</summary>
    /// <param name="message">The broker delivery to process.</param>
    /// <param name="cancellationToken">Cancels pipeline processing.</param>
    /// <returns>The dispatch task for the configured receive pipeline.</returns>
    Task HandleAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default);
}
