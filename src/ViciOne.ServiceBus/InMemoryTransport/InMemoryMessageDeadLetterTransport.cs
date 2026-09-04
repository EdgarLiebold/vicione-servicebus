using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

public class InMemoryMessageDeadLetterTransport :
    InMemoryMessageMoveTransport,
    IDeadLetterTransport
{
    public InMemoryMessageDeadLetterTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
        : base(exchange, delayProvider)
    {
    }

    public Task Send(ReceiveContext context, string reason)
    {
        void PreSend(InMemoryTransportMessage message, SendHeaders headers)
        {
            headers.Set(MessageHeaders.Reason, reason ?? "Unspecified");
        }

        return Move(context, PreSend);
    }
}
