using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Copies faulted Azure Service Bus deliveries to the configured error queue.</summary>
public class ServiceBusQueueErrorTransport :
    ServiceBusQueueMoveTransport,
    IErrorTransport
{
    /// <summary>Creates an error transport for a destination entity.</summary>
    /// <param name="supervisor">The namespace connection supervisor.</param>
    /// <param name="settings">The error-queue declaration and sender settings.</param>
    public ServiceBusQueueErrorTransport(IConnectionContextSupervisor supervisor, SendSettings settings)
        : base(supervisor, settings)
    {
    }

    /// <summary>Copies a faulted delivery to the error queue with exception headers and a bounded time to live.</summary>
    /// <param name="context">The failed receive context.</param>
    /// <param name="cancellationToken">Rejects the move when cancellation has already been requested.</param>
    /// <returns>A task that completes when the copied message has been sent.</returns>
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
