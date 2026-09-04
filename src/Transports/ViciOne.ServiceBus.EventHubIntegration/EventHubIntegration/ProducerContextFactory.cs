using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.EventHubIntegration;

public class ProducerContextFactory :
    IPipeContextFactory<ProducerContext>
{
    readonly IConnectionContextSupervisor _contextSupervisor;
    readonly string _eventHubName;

    public ProducerContextFactory(IConnectionContextSupervisor contextSupervisor, string eventHubName)
    {
        _contextSupervisor = contextSupervisor;
        _eventHubName = eventHubName;
    }

    public IActivePipeContextAgent<ProducerContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<ProducerContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnectionAsync(context.Context, cancellationToken));
    }

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
