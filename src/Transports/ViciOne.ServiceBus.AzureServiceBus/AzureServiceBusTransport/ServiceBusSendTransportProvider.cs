using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Normalizes Azure Service Bus addresses and resolves send transports for a receive endpoint.</summary>
public class ServiceBusSendTransportProvider :
    ISendTransportProvider
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly ReceiveEndpointContext _context;

    /// <summary>Creates a provider bound to a namespace connection and receive endpoint.</summary>
    /// <param name="connectionContextSupervisor">The namespace connection supervisor.</param>
    /// <param name="context">The receive endpoint requesting send transports.</param>
    public ServiceBusSendTransportProvider(IConnectionContextSupervisor connectionContextSupervisor, ReceiveEndpointContext context)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _context = context;
    }

    /// <summary>Resolves a relative or transport-specific address against the configured namespace.</summary>
    /// <param name="address">The endpoint address to normalize.</param>
    /// <returns>The absolute Azure Service Bus endpoint URI.</returns>
    public Uri NormalizeAddress(Uri address)
    {
        return _connectionContextSupervisor.NormalizeAddress(address);
    }

    Task<ISendTransport> ISendTransportProvider.GetSendTransportAsync(Uri address, CancellationToken cancellationToken)
    {
        return _connectionContextSupervisor.CreateSendTransportAsync(_context, address, cancellationToken: cancellationToken);
    }
}
