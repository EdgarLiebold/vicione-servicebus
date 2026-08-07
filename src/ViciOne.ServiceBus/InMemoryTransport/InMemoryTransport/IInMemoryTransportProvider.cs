// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.InMemoryTransport
{
    using System;
    using System.Threading.Tasks;
    using Transports;
    using Transports.Fabric;


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
}
