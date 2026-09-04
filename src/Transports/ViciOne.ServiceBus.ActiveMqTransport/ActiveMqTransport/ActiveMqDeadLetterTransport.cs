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

    public Task Send(ReceiveContext context, string reason)
    {
        void PreSend(IMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return Move(context, PreSend);
    }
}
