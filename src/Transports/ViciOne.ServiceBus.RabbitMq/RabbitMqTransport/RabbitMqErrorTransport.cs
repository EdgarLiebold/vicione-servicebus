using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Republishes faulted deliveries to the configured RabbitMQ error exchange.</summary>
public class RabbitMqErrorTransport :
    RabbitMqMoveTransport<ErrorSettings>,
    IErrorTransport
{
    /// <summary>Creates a move transport for an error exchange and its topology.</summary>
    /// <param name="exchange">The destination exchange name.</param>
    /// <param name="topologyFilter">The filter that declares error topology before publishing.</param>
    public RabbitMqErrorTransport(string exchange, ConfigureRabbitMqTopologyFilter<ErrorSettings> topologyFilter)
        : base(exchange, topologyFilter)
    {
    }

    /// <summary>Copies a faulted message and its exception headers to the error exchange.</summary>
    /// <param name="context">The faulted receive context to copy.</param>
    /// <param name="cancellationToken">Cancellation checked before the move begins; the receive context governs the broker operations.</param>
    /// <returns>A task that follows the mandatory RabbitMQ client publish operation; the receive pipeline settles the source delivery separately.</returns>
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
