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

    Task<ISendTransport> CreateSendTransport(ActiveMqReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor, Uri address);

    Task<ISendTransport> CreatePublishTransport<T>(ActiveMqReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor)
        where T : class;
}
