using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Supervises Amazon connection contexts and creates address-specific send transports.</summary>
public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    /// <summary>Resolves an endpoint address relative to the configured Amazon SQS host.</summary>
    /// <param name="address">The absolute or relative endpoint address.</param>
    /// <returns>The normalized absolute endpoint address.</returns>
    Uri NormalizeAddress(Uri address);

    /// <summary>Creates an Amazon SQS queue transport or Amazon SNS topic transport for an endpoint address.</summary>
    /// <param name="receiveEndpointContext">The receive endpoint requesting the transport.</param>
    /// <param name="clientContextSupervisor">The client supervisor that owns the transport.</param>
    /// <param name="address">The queue or topic endpoint address.</param>
    /// <param name="cancellationToken">The token checked before transport creation.</param>
    /// <returns>The created send transport.</returns>
    Task<ISendTransport> CreateSendTransportAsync(SqsReceiveEndpointContext receiveEndpointContext, IClientContextSupervisor clientContextSupervisor,
        Uri address, CancellationToken cancellationToken = default);

    /// <summary>Creates an Amazon SNS publish transport for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <param name="receiveEndpointContext">The receive endpoint requesting the transport.</param>
    /// <param name="clientContextSupervisor">The client supervisor that owns the transport.</param>
    /// <param name="cancellationToken">The token checked before transport creation.</param>
    /// <returns>The created publish transport.</returns>
    Task<ISendTransport> CreatePublishTransportAsync<T>(SqsReceiveEndpointContext receiveEndpointContext, IClientContextSupervisor clientContextSupervisor, CancellationToken cancellationToken = default)
        where T : class;
}
