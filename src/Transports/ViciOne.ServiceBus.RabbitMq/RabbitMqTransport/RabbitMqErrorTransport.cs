using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq error transport implementation.
/// </summary>
public class RabbitMqErrorTransport :
    RabbitMqMoveTransport<ErrorSettings>,
    IErrorTransport
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="topologyFilter">The topology filter value.</param>
    public RabbitMqErrorTransport(string exchange, ConfigureRabbitMqTopologyFilter<ErrorSettings> topologyFilter)
        : base(exchange, topologyFilter)
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(BasicProperties message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);

            message.ClearExpiration();
        }

        return MoveAsync(context, PreSend);
    }
}
