using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    Uri NormalizeAddress(Uri address);

    Task<ISendTransport> CreateSendTransport(SqsReceiveEndpointContext receiveEndpointContext, IClientContextSupervisor clientContextSupervisor,
        Uri address);

    Task<ISendTransport> CreatePublishTransport<T>(SqsReceiveEndpointContext receiveEndpointContext, IClientContextSupervisor clientContextSupervisor)
        where T : class;
}
