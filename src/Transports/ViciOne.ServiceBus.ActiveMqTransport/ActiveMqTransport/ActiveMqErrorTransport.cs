using System.Threading.Tasks;
using Apache.NMS;
using ViciOne.ServiceBus.ActiveMqTransport.Middleware;
using ViciOne.ServiceBus.ActiveMqTransport.Topology;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

public class ActiveMqErrorTransport :
    ActiveMqMoveTransport<ErrorSettings>,
    IErrorTransport
{
    public ActiveMqErrorTransport(Queue destination, ConfigureActiveMqTopologyFilter<ErrorSettings> topologyFilter)
        : base(destination, topologyFilter)
    {
    }

    public Task SendAsync(ExceptionReceiveContext context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); void PreSend(IMessage message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);
        }

        return MoveAsync(context, PreSend);
    }
}
