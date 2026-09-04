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

    Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext context, Uri address, CancellationToken cancellationToken = default);

    Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext context, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class;

    Uri NormalizeAddress(Uri address);
}
