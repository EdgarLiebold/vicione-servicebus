using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Creates supervised Azure Service Bus sender contexts and declares their destination topology.</summary>
public class SendEndpointContextFactory :
    IPipeContextFactory<SendEndpointContext>
{
    readonly ConfigureServiceBusTopologyFilter<SendSettings> _configureTopologyFilter;
    readonly SendSettings _settings;
    readonly IConnectionContextSupervisor _supervisor;

    /// <summary>Creates a factory for one destination entity.</summary>
    /// <param name="supervisor">The namespace connection supervisor.</param>
    /// <param name="configureTopologyFilter">The filter that declares or validates destination topology.</param>
    /// <param name="settings">The destination entity and sender settings.</param>
    public SendEndpointContextFactory(IConnectionContextSupervisor supervisor, ConfigureServiceBusTopologyFilter<SendSettings> configureTopologyFilter,
        SendSettings settings)
    {
        _supervisor = supervisor;
        _configureTopologyFilter = configureTopologyFilter;
        _settings = settings;
    }

    /// <summary>Creates an asynchronous context agent and starts sender creation.</summary>
    /// <param name="supervisor">The owner of the context agent.</param>
    /// <returns>The agent that completes with the sender context.</returns>
    public IPipeContextAgent<SendEndpointContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<SendEndpointContext> asyncContext = supervisor.AddAsyncContext<SendEndpointContext>();

        CreateSendEndpointContext(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    /// <summary>Creates a shared lease over an existing sender context.</summary>
    /// <param name="supervisor">The supervisor that owns the active lease.</param>
    /// <param name="context">The sender-context handle to share.</param>
    /// <param name="cancellationToken">Cancels acquisition and bounds the lease lifetime.</param>
    /// <returns>The active shared-context agent.</returns>
    public IActivePipeContextAgent<SendEndpointContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<SendEndpointContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedContextAsync(context.Context, cancellationToken));
    }

    void CreateSendEndpointContext(IAsyncPipeContextAgent<SendEndpointContext> asyncContext, CancellationToken cancellationToken)
    {
        async Task<SendEndpointContext> CreateAsync(ConnectionContext context, CancellationToken createCancellationToken)
        {
            var messageSender = context.CreateMessageSender(_settings.EntityPath);

            var sendEndpointContext = new MessageSendEndpointContext(context, messageSender);

            await _configureTopologyFilter.ConfigureAsync(sendEndpointContext, createCancellationToken).ConfigureAwait(false);

            return sendEndpointContext;
        }

        _supervisor.StartAgent(asyncContext, CreateAsync, cancellationToken);
    }

    static async Task<SendEndpointContext> CreateSharedContextAsync(Task<SendEndpointContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedSendEndpointContext(context.Result, cancellationToken)
            : new SharedSendEndpointContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }
}
