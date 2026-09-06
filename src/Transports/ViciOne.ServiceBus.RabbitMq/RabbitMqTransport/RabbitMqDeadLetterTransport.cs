using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Republishes skipped deliveries to the configured RabbitMQ dead-letter exchange.</summary>
public class RabbitMqDeadLetterTransport :
    RabbitMqMoveTransport<DeadLetterSettings>,
    IDeadLetterTransport
{
    /// <summary>Creates a move transport for a dead-letter exchange and its topology.</summary>
    /// <param name="exchange">The destination exchange name.</param>
    /// <param name="topologyFilter">The filter that declares dead-letter topology before publishing.</param>
    public RabbitMqDeadLetterTransport(string exchange, ConfigureRabbitMqTopologyFilter<DeadLetterSettings> topologyFilter)
        : base(exchange, topologyFilter)
    {
    }

    /// <summary>Copies a received message to the dead-letter exchange with a reason header.</summary>
    /// <param name="context">The received message to copy.</param>
    /// <param name="reason">The dead-letter reason, or <c>Unspecified</c> when absent.</param>
    /// <param name="cancellationToken">Cancellation checked before the move begins; the receive context governs the broker operations.</param>
    /// <returns>A task that follows the mandatory RabbitMQ client publish operation; the receive pipeline settles the source delivery separately.</returns>
    public Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(BasicProperties message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
