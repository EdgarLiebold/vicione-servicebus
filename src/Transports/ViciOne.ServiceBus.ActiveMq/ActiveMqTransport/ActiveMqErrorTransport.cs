using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Middleware;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Moves faulted ActiveMQ messages to the configured error queue.</summary>
public class ActiveMqErrorTransport :
    ActiveMqMoveTransport<ErrorSettings>,
    IErrorTransport
{
    /// <summary>Creates an error transport for an ActiveMQ queue.</summary>
    /// <param name="destination">The broker queue that receives faulted messages.</param>
    /// <param name="topologyFilter">The filter that provisions the error topology.</param>
    public ActiveMqErrorTransport(Queue destination, ConfigureActiveMqTopologyFilter<ErrorSettings> topologyFilter)
        : base(destination, topologyFilter)
    {
    }

    /// <summary>Moves a faulted received message to the error queue and copies its exception headers.</summary>
    /// <param name="context">The received-message exception context.</param>
    /// <param name="cancellationToken">The token checked before the move begins.</param>
    /// <returns>A task that completes when the message has been moved.</returns>
    public Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(IMessage message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);
        }

        return MoveAsync(context, PreSend);
    }
}
