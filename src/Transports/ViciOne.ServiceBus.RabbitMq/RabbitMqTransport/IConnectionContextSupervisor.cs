using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Supervises RabbitMQ connections and creates send transports from shared channels.</summary>
public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    /// <summary>Resolves a full or short destination address against the configured host.</summary>
    /// <param name="address">The destination address to resolve.</param>
    /// <returns>The full RabbitMQ destination URI.</returns>
    Uri NormalizeAddress(Uri address);

    /// <summary>Creates a send transport and its destination topology.</summary>
    /// <param name="receiveEndpointContext">The endpoint context that owns the transport.</param>
    /// <param name="channelContextSupervisor">The shared RabbitMQ channel supervisor.</param>
    /// <param name="address">The full or short destination address.</param>
    /// <param name="cancellationToken">Cancellation checked before transport creation.</param>
    /// <returns>The configured send transport.</returns>
    Task<ISendTransport> CreateSendTransportAsync(RabbitMqReceiveEndpointContext receiveEndpointContext, IChannelContextSupervisor channelContextSupervisor,
        Uri address, CancellationToken cancellationToken = default);

    /// <summary>Creates a publish transport and topology for a message contract.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="receiveEndpointContext">The endpoint context that owns the transport.</param>
    /// <param name="channelContextSupervisor">The shared RabbitMQ channel supervisor.</param>
    /// <param name="cancellationToken">Cancellation checked before transport creation.</param>
    /// <returns>The configured publish transport.</returns>
    Task<ISendTransport> CreatePublishTransportAsync<T>(RabbitMqReceiveEndpointContext receiveEndpointContext, IChannelContextSupervisor channelContextSupervisor, CancellationToken cancellationToken = default)
        where T : class;
}
