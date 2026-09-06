using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Supervises an Azure Service Bus namespace connection and its dependent contexts.</summary>
public interface IConnectionContextSupervisor :
    ITransportSupervisor<ConnectionContext>
{
    /// <summary>Creates a processor-client supervisor bound to this namespace connection.</summary>
    /// <param name="factory">Creates client contexts when the supervisor reconnects.</param>
    /// <returns>The new client-context supervisor.</returns>
    IClientContextSupervisor CreateClientContextSupervisor(Func<IConnectionContextSupervisor, IPipeContextFactory<ClientContext>> factory);

    /// <summary>Creates a send-endpoint supervisor for an Azure entity.</summary>
    /// <param name="settings">The entity declaration and sender settings.</param>
    /// <returns>The new send-endpoint supervisor.</returns>
    ISendEndpointContextSupervisor CreateSendEndpointContextSupervisor(SendSettings settings);

    /// <summary>Creates a transport that sends to a queue or topic address.</summary>
    /// <param name="context">The receive endpoint requesting the transport.</param>
    /// <param name="address">The destination endpoint address.</param>
    /// <param name="cancellationToken">Cancels endpoint-context acquisition.</param>
    /// <returns>A task that produces the send transport.</returns>
    Task<ISendTransport> CreateSendTransportAsync(ReceiveEndpointContext context, Uri address, CancellationToken cancellationToken = default);

    /// <summary>Creates a transport that publishes a message contract to its topic.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="context">The receive endpoint requesting the transport.</param>
    /// <param name="publishAddress">The topic address.</param>
    /// <param name="cancellationToken">Cancels endpoint-context acquisition.</param>
    /// <returns>A task that produces the publish transport.</returns>
    Task<ISendTransport> CreatePublishTransportAsync<T>(ReceiveEndpointContext context, Uri publishAddress, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Resolves a relative or transport-specific address against the configured namespace.</summary>
    /// <param name="address">The endpoint address to normalize.</param>
    /// <returns>The absolute Azure Service Bus endpoint URI.</returns>
    Uri NormalizeAddress(Uri address);
}
