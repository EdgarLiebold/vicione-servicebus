using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class ServiceBusQueueErrorTransport :
    ServiceBusQueueMoveTransport,
    IErrorTransport
{
    public ServiceBusQueueErrorTransport(IConnectionContextSupervisor supervisor, SendSettings settings)
        : base(supervisor, settings)
    {
    }

    public Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(ServiceBusMessage message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);

            message.TimeToLive = Defaults.BasicMessageTimeToLive;
        }

        return MoveAsync(context, PreSend);
    }
}
