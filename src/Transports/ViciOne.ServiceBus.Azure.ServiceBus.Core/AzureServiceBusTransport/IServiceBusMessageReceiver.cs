using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public interface IServiceBusMessageReceiver
{
    /// <summary>
    /// Handles the <paramref name="message" />
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken">Specify an optional cancellationToken</param>
    /// <returns></returns>
    Task Handle(ServiceBusReceivedMessage message, CancellationToken cancellationToken = default);
}
