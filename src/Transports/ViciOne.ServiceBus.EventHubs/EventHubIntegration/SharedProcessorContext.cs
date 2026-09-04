using System.Threading;
using Azure.Messaging.EventHubs;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a shared processor context implementation.
/// </summary>
public class SharedProcessorContext :
    ProxyPipeContext,
    ProcessorContext
{
    readonly ProcessorContext _context;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public SharedProcessorContext(ProcessorContext context, CancellationToken cancellationToken)
        : base(context)
    {
        _context = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the cancellation token value.
    /// </summary>
    public override CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the log context value.
    /// </summary>
    public ILogContext LogContext => _context.LogContext;

    /// <summary>
    /// Gets client.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
    public EventProcessorClient GetClient(ProcessorClientBuilderContext context)
    {
        return _context.GetClient(context);
    }

    /// <summary>
    /// Performs the release client operation.
    /// </summary>
    /// <param name="processorLockContext">The processor lock context value.</param>
    public void ReleaseClient(ProcessorClientBuilderContext processorLockContext)
    {
        _context.ReleaseClient(processorLockContext);
    }
}
