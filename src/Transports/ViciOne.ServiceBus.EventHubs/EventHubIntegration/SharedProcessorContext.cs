using System.Threading;
using Azure.Messaging.EventHubs;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Projects a shared Event Hubs processor context onto a caller-specific cancellation token.</summary>
public class SharedProcessorContext :
    ProxyPipeContext,
    ProcessorContext
{
    readonly ProcessorContext _context;

    /// <summary>Creates a proxy over a shared processor context.</summary>
    /// <param name="context">The shared processor context.</param>
    /// <param name="cancellationToken">The cancellation token exposed by this proxy.</param>
    public SharedProcessorContext(ProcessorContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>Gets the cancellation token.</summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>Gets the underlying receive endpoint's logging context.</summary>
    public ILogContext LogContext => _context.LogContext;

    /// <summary>Registers partition callbacks and leases the shared processor client.</summary>
    /// <param name="context">The callback target for partition initialization and closure.</param>
    /// <returns>The shared processor client.</returns>
    public EventProcessorClient GetClient(ProcessorClientBuilderContext context)
    {
        return _context.GetClient(context);
    }

    /// <summary>Releases the processor client lease and its partition callback subscriptions.</summary>
    public void ReleaseClient()
    {
        _context.ReleaseClient();
    }
}
