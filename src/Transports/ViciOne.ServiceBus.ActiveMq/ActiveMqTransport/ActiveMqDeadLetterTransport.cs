using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Middleware;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Moves skipped ActiveMQ messages to the configured dead-letter queue.</summary>
public class ActiveMqDeadLetterTransport :
    ActiveMqMoveTransport<DeadLetterSettings>,
    IDeadLetterTransport
{
    /// <summary>Creates a dead-letter transport for an ActiveMQ queue.</summary>
    /// <param name="destination">The broker queue that receives skipped messages.</param>
    /// <param name="topologyFilter">The filter that provisions the dead-letter topology.</param>
    public ActiveMqDeadLetterTransport(Queue destination, ConfigureActiveMqTopologyFilter<DeadLetterSettings> topologyFilter)
        : base(destination, topologyFilter)
    {
    }

    /// <summary>Moves a received message to the dead-letter queue and records the reason.</summary>
    /// <param name="context">The received-message context.</param>
    /// <param name="reason">The reason the message was skipped.</param>
    /// <param name="cancellationToken">The token checked before the move begins.</param>
    /// <returns>A task that completes when the message has been moved.</returns>
    public Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(IMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
