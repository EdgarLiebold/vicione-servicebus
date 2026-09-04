using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a producer context factory implementation.
/// </summary>
public class ProducerContextFactory :
    IPipeContextFactory<ProducerContext>
{
    readonly IConnectionContextSupervisor _contextSupervisor;
    readonly string _eventHubName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="contextSupervisor">The context supervisor value.</param>
    /// <param name="eventHubName">The event hub name value.</param>
    public ProducerContextFactory(IConnectionContextSupervisor contextSupervisor, string eventHubName)
    {
        _contextSupervisor = contextSupervisor;
        _eventHubName = eventHubName;
    }

    /// <summary>
    /// Creates active context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IActivePipeContextAgent<ProducerContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<ProducerContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnectionAsync(context.Context, cancellationToken));
    }

    /// <summary>
    /// Creates context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipeContextAgent<ProducerContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ProducerContext> asyncContext = supervisor.AddAsyncContext<ProducerContext>();

        CreateProcessor(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    static async Task<ProducerContext> CreateSharedConnectionAsync(Task<ProducerContext> context,
        CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedProducerContext(context.Result, cancellationToken)
            : new SharedProducerContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    void CreateProcessor(IAsyncPipeContextAgent<ProducerContext> asyncContext, CancellationToken cancellationToken)
    {
        Task<ProducerContext> CreateAsync(ConnectionContext connectionContext, CancellationToken createCancellationToken)
        {
            var client = connectionContext.CreateEventHubClient(_eventHubName);
            ProducerContext context = new EventHubProducerContext(client, createCancellationToken);
            return Task.FromResult(context);
        }

        _contextSupervisor.StartAgent(asyncContext, CreateAsync, cancellationToken);
    }
}
