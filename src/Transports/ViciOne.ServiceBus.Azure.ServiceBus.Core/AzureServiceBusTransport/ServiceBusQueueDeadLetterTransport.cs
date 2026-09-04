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

    public Task Send(ReceiveContext context, string reason)
    {
        void PreSend(ServiceBusMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return Move(context, PreSend);
    }
}
