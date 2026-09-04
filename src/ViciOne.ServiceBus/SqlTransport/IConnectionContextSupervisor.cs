using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    Task<ISendTransport> CreateSendTransportAsync(SqlReceiveEndpointContext context, Uri address, CancellationToken cancellationToken = default);

    Task<ISendTransport> CreatePublishTransportAsync<T>(SqlReceiveEndpointContext context, Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class;

    Uri NormalizeAddress(Uri address);
}
