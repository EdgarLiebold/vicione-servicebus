using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqsTransport;

public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    Uri NormalizeAddress(Uri address);

    Task<ISendTransport> CreateSendTransportAsync(SqsReceiveEndpointContext receiveEndpointContext, IClientContextSupervisor clientContextSupervisor,
        Uri address, CancellationToken cancellationToken = default);

    Task<ISendTransport> CreatePublishTransportAsync<T>(SqsReceiveEndpointContext receiveEndpointContext, IClientContextSupervisor clientContextSupervisor, CancellationToken cancellationToken = default)
        where T : class;
}
