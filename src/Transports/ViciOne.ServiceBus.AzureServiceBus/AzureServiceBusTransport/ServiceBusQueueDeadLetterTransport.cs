using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Copies skipped Azure Service Bus deliveries to the configured dead-letter destination.</summary>
public class ServiceBusQueueDeadLetterTransport :
    ServiceBusQueueMoveTransport,
    IDeadLetterTransport
{
    /// <summary>Creates a dead-letter transport for a destination entity.</summary>
    /// <param name="supervisor">The namespace connection supervisor.</param>
    /// <param name="settings">The dead-letter entity declaration and sender settings.</param>
    public ServiceBusQueueDeadLetterTransport(IConnectionContextSupervisor supervisor, SendSettings settings)
        : base(supervisor, settings)
    {
    }

    /// <summary>Copies a skipped delivery to the dead-letter destination with its reason header.</summary>
    /// <param name="context">The skipped receive context.</param>
    /// <param name="reason">The reason stored with the copied message.</param>
    /// <param name="cancellationToken">Rejects the move when cancellation has already been requested.</param>
    /// <returns>A task that completes when the copied message has been sent.</returns>
    public Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(ServiceBusMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
