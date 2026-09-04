using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ServiceBusQueueDeadLetterTransport :
    ServiceBusQueueMoveTransport,
    IDeadLetterTransport
{
    public ServiceBusQueueDeadLetterTransport(IConnectionContextSupervisor supervisor, SendSettings settings)
        : base(supervisor, settings)
    {
    }

    public Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(ServiceBusMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
