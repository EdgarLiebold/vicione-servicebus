using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Resolves Azure Service Bus publish transports for a receive endpoint.</summary>
public class ServiceBusPublishTransportProvider :
    IPublishTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly ReceiveEndpointContext _context;

    /// <summary>Creates a provider bound to a namespace connection and receive endpoint.</summary>
    /// <param name="connectionContextSupervisor">The namespace connection supervisor.</param>
    /// <param name="context">The receive endpoint requesting publish transports.</param>
    public ServiceBusPublishTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, ReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
    }

    /// <summary>Gets a transport that publishes a message contract to its Azure Service Bus topic.</summary>
    /// <typeparam name="T">The published message contract.</typeparam>
    /// <param name="publishAddress">The topic address resolved by publish topology.</param>
    /// <param name="cancellationToken">Cancels transport acquisition.</param>
    /// <returns>A task that produces the publish transport.</returns>
    public Task<ISendTransport> GetPublishTransportAsync<T>(Uri? publishAddress, CancellationToken cancellationToken = default)
        where T : class
    {
        return _connectionContextSupervisor.CreatePublishTransportAsync<T>(_context, publishAddress!, cancellationToken: cancellationToken);
    }
}
