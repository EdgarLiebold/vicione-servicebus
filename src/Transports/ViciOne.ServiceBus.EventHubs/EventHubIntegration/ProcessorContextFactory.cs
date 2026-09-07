using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Creates and shares supervised Event Hubs processor contexts for a receive endpoint.</summary>
public class ProcessorContextFactory :
    IPipeContextFactory<ProcessorContext>
{
    readonly Func<EventProcessorClient> _clientFactory;
    readonly IConnectionContextSupervisor _contextSupervisor;
    readonly IHostConfiguration _hostConfiguration;
    readonly Func<PartitionClosingEventArgs, Task>? _partitionClosingHandler;
    readonly Func<PartitionInitializingEventArgs, Task>? _partitionInitializingHandler;

    /// <summary>Creates a processor-context factory from connection, client, logging, and partition callback dependencies.</summary>
    /// <param name="contextSupervisor">The shared Event Hubs connection supervisor.</param>
    /// <param name="hostConfiguration">The bus host configuration used for logging.</param>
    /// <param name="clientFactory">Creates the Azure SDK event processor client.</param>
    /// <param name="partitionClosingHandler">The optional application partition-closing handler.</param>
    /// <param name="partitionInitializingHandler">The optional application partition-initializing handler.</param>
    public ProcessorContextFactory(IConnectionContextSupervisor contextSupervisor, IHostConfiguration hostConfiguration,
        Func<EventProcessorClient> clientFactory,
        Func<PartitionClosingEventArgs, Task>? partitionClosingHandler,
        Func<PartitionInitializingEventArgs, Task>? partitionInitializingHandler)
    {
        _contextSupervisor = contextSupervisor;
        _hostConfiguration = hostConfiguration;
        _clientFactory = clientFactory;
        _partitionClosingHandler = partitionClosingHandler;
        _partitionInitializingHandler = partitionInitializingHandler;
    }

    /// <summary>Creates a caller-scoped active agent over an existing processor context.</summary>
    /// <param name="supervisor">The supervisor that will own the active agent.</param>
    /// <param name="context">The handle for the shared processor context.</param>
    /// <param name="cancellationToken">The cancellation token exposed by the scoped context.</param>
    /// <returns>The active processor-context agent.</returns>
    public IActivePipeContextAgent<ProcessorContext> CreateActiveContext(ISupervisor supervisor, IPipeContextHandle<ProcessorContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnectionAsync(context.Context, cancellationToken));
    }

    IPipeContextAgent<ProcessorContext> IPipeContextFactory<ProcessorContext>.CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ProcessorContext> asyncContext = supervisor.AddAsyncContext<ProcessorContext>();

        CreateProcessor(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    static async Task<ProcessorContext> CreateSharedConnectionAsync(Task<ProcessorContext> context,
        CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedProcessorContext(context.Result, cancellationToken)
            : new SharedProcessorContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    void CreateProcessor(IAsyncPipeContextAgent<ProcessorContext> asyncContext, CancellationToken cancellationToken)
    {
        Task<ProcessorContext> CreateAsync(ConnectionContext connectionContext, CancellationToken createCancellationToken)
        {
            var client = _clientFactory();

            ProcessorContext context = new EventHubProcessorContext(_hostConfiguration, client, _partitionInitializingHandler, _partitionClosingHandler,
                createCancellationToken);
            return Task.FromResult(context);
        }

        _contextSupervisor.StartAgent(asyncContext, CreateAsync, cancellationToken);
    }
}
