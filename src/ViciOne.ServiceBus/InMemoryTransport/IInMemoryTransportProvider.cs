using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport;

public interface IInMemoryTransportProvider :
    InMemoryTransportContext,
    IAgent,
    IProbeSite
{
    IMessageFabric<InMemoryTransportContext, InMemoryTransportMessage> MessageFabric { get; }

    Task<ISendTransport> CreateSendTransport(ReceiveEndpointContext context, Uri address);

    Task<ISendTransport> CreatePublishTransport<T>(ReceiveEndpointContext context, Uri publishAddress)
        where T : class;

    Uri NormalizeAddress(Uri address);
}
