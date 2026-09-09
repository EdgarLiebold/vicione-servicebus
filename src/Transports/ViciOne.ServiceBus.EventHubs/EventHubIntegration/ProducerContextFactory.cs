using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Creates and shares supervised producer contexts for one Event Hub.</summary>
public class ProducerContextFactory :
    IPipeContextFactory<ProducerContext>
{
    readonly IConnectionContextSupervisor _contextSupervisor;
    readonly string _eventHubName;

    /// <summary>Creates a producer-context factory for a named Event Hub.</summary>
    /// <param name="contextSupervisor">The shared Event Hubs connection supervisor.</param>
    /// <param name="eventHubName">The Event Hub entity name.</param>
    public ProducerContextFactory(IConnectionContextSupervisor contextSupervisor, string eventHubName)
    {
        _contextSupervisor = contextSupervisor;
        _eventHubName = eventHubName;
    }

    /// <summary>Creates a caller-scoped active agent over an existing producer context.</summary>
    /// <param name="supervisor">The supervisor that will own the active agent.</param>
    /// <param name="context">The handle for the shared producer context.</param>
    /// <param name="cancellationToken">The cancellation token exposed by the scoped context.</param>
    /// <returns>The active producer-context agent.</returns>
    public IActivePipeContextAgent<ProducerContext> CreateActiveContext(ISupervisor supervisor, IPipeContextHandle<ProducerContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnectionAsync(context.Context, cancellationToken));
    }

    /// <summary>Creates an asynchronous agent that acquires a producer client through the connection supervisor.</summary>
    /// <param name="supervisor">The supervisor that will own the context agent.</param>
    /// <returns>The asynchronous producer-context agent.</returns>
    public IPipeContextAgent<ProducerContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ProducerContext> asyncContext = supervisor.AddAsyncContext<ProducerContext>();

        CreateProcessor(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    static async Task<ProducerContext> CreateSharedConnectionAsync(Task<ProducerContext> context,
        CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully
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
