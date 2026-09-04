using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

public class InMemoryMessageErrorTransport :
    InMemoryMessageMoveTransport,
    IErrorTransport
{
    public InMemoryMessageErrorTransport(IMessageExchange<InMemoryTransportMessage> exchange, IInMemoryDelayProvider delayProvider)
        : base(exchange, delayProvider)
    {
    }

    public Task Send(ExceptionReceiveContext context)
    {
        void PreSend(InMemoryTransportMessage message, SendHeaders headers)
        {
            headers.CopyFrom(context.ExceptionHeaders);
        }

        return Move(context, PreSend);
    }
}
