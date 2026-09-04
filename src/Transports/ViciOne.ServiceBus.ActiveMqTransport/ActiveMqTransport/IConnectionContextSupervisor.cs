using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMqTransport;

/// <summary>
/// Attaches a connection context to the value (shared, of course)
/// </summary>
public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    Uri NormalizeAddress(Uri address);

    Task<ISendTransport> CreateSendTransportAsync(ActiveMqReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor, Uri address, CancellationToken cancellationToken = default);

    Task<ISendTransport> CreatePublishTransportAsync<T>(ActiveMqReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor, CancellationToken cancellationToken = default)
        where T : class;
}
