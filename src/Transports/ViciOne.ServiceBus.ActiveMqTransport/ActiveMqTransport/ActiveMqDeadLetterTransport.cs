using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMqTransport.Middleware;
using ViciOne.ServiceBus.ActiveMqTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ActiveMqDeadLetterTransport :
    ActiveMqMoveTransport<DeadLetterSettings>,
    IDeadLetterTransport
{
    public ActiveMqDeadLetterTransport(Queue destination, ConfigureActiveMqTopologyFilter<DeadLetterSettings> topologyFilter)
        : base(destination, topologyFilter)
    {
    }

    public Task SendAsync(ReceiveContext context, string reason, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(IMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return MoveAsync(context, PreSend);
    }
}
