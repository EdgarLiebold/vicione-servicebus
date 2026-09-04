using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides a processor context factory implementation.
/// </summary>
public class ProcessorContextFactory :
    IPipeContextFactory<ProcessorContext>
{
    readonly Func<EventProcessorClient> _clientFactory;
    readonly IConnectionContextSupervisor _contextSupervisor;
    readonly IHostConfiguration _hostConfiguration;
    readonly Func<PartitionClosingEventArgs, Task>? _partitionClosingHandler;
    readonly Func<PartitionInitializingEventArgs, Task>? _partitionInitializingHandler;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="contextSupervisor">The context supervisor value.</param>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="clientFactory">The client factory value.</param>
    /// <param name="partitionClosingHandler">The partition closing handler value.</param>
    /// <param name="partitionInitializingHandler">The partition initializing handler value.</param>
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

    /// <summary>
    /// Creates active context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IActivePipeContextAgent<ProcessorContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<ProcessorContext> context,
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
