using System.Threading;
using Azure.Messaging.EventHubs.Producer;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Projects a shared Event Hubs connection context onto a caller-specific cancellation token.</summary>
public class SharedConnectionContext :
    ProxyPipeContext,
    ConnectionContext
{
    readonly ConnectionContext _context;

    /// <summary>Creates a proxy over a shared connection context.</summary>
    /// <param name="context">The shared connection context.</param>
    /// <param name="cancellationToken">The cancellation token exposed by this proxy.</param>
    public SharedConnectionContext(ConnectionContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the cancellation token.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Creates a producer client through the shared connection context.</summary>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    /// <returns>The producer client for the entity.</returns>
    public EventHubProducerClient CreateEventHubClient(string eventHubName)
    {
        return _context.CreateEventHubClient(eventHubName);
    }
}
