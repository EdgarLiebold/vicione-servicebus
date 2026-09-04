using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Defines the contract for service bus message receiver.
/// </summary>
public interface IServiceBusMessageReceiver
{
    /// <summary>
    /// Handles the <paramref name="message" />
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken">Specify an optional cancellationToken</param>
    /// <returns></returns>
    Task HandleAsync(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default);
}
