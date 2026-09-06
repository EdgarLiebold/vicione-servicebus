using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Supervises ActiveMQ connections and creates destination-specific send transports.</summary>
public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    /// <summary>Normalizes a destination address against the configured broker host.</summary>
    /// <param name="address">The queue, topic, or absolute destination address.</param>
    /// <returns>The canonical absolute ActiveMQ destination URI.</returns>
    Uri NormalizeAddress(Uri address);

    /// <summary>Creates a send transport for an ActiveMQ queue or topic address.</summary>
    /// <param name="context">The receive-endpoint context that owns the transport.</param>
    /// <param name="sessionContextSupervisor">The parent session supervisor.</param>
    /// <param name="address">The destination address.</param>
    /// <param name="cancellationToken">The token checked before transport creation.</param>
    /// <returns>A task that produces the configured send transport.</returns>
    Task<ISendTransport> CreateSendTransportAsync(ActiveMqReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor, Uri address, CancellationToken cancellationToken = default);

    /// <summary>Creates a topic send transport from a message type's publish topology.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <param name="context">The receive-endpoint context that owns the transport.</param>
    /// <param name="sessionContextSupervisor">The parent session supervisor.</param>
    /// <param name="cancellationToken">The token checked before transport creation.</param>
    /// <returns>A task that produces the configured publish transport.</returns>
    Task<ISendTransport> CreatePublishTransportAsync<T>(ActiveMqReceiveEndpointContext context, ISessionContextSupervisor sessionContextSupervisor, CancellationToken cancellationToken = default)
        where T : class;
}
