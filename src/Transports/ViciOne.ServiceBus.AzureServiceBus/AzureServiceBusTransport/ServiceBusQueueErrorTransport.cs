using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a service bus queue error transport implementation.
/// </summary>
public class ServiceBusQueueErrorTransport :
    ServiceBusQueueMoveTransport,
    IErrorTransport
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="settings">The settings value.</param>
    public ServiceBusQueueErrorTransport(IConnectionContextSupervisor supervisor, SendSettings settings)
        : base(supervisor, settings)
    {
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
