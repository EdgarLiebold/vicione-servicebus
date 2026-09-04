using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.AzureServiceBus.Middleware;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a send endpoint context factory implementation.
/// </summary>
public class SendEndpointContextFactory :
    IPipeContextFactory<SendEndpointContext>
{
    readonly ConfigureServiceBusTopologyFilter<SendSettings> _configureTopologyFilter;
    readonly SendSettings _settings;
    readonly IConnectionContextSupervisor _supervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="configureTopologyFilter">The configure topology filter value.</param>
    /// <param name="settings">The settings value.</param>
    public SendEndpointContextFactory(IConnectionContextSupervisor supervisor, ConfigureServiceBusTopologyFilter<SendSettings> configureTopologyFilter,
        SendSettings settings)
    {
        _supervisor = supervisor;
        _configureTopologyFilter = configureTopologyFilter;
        _settings = settings;
    }

    /// <summary>
    /// Creates context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <returns>The result of the operation.</returns>
    public IPipeContextAgent<SendEndpointContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<SendEndpointContext> asyncContext = supervisor.AddAsyncContext<SendEndpointContext>();

        CreateSendEndpointContext(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    /// <summary>
    /// Creates active context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
