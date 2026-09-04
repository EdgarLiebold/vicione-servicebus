using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMq.Middleware;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides an active mq error transport implementation.
/// </summary>
public class ActiveMqErrorTransport :
    ActiveMqMoveTransport<ErrorSettings>,
    IErrorTransport
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="destination">The destination value.</param>
    /// <param name="topologyFilter">The topology filter value.</param>
    public ActiveMqErrorTransport(Queue destination, ConfigureActiveMqTopologyFilter<ErrorSettings> topologyFilter)
        : base(destination, topologyFilter)
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
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(IMessage message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);
        }

        return MoveAsync(context, PreSend);
    }
}
