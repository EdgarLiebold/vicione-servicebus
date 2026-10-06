using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Creates shared and operation-scoped Amazon client contexts.</summary>
public class ClientContextFactory :
    IPipeContextFactory<ClientContext>
{
    readonly IConnectionContextSupervisor _connectionContextSupervisor;
    readonly Func<IPipe<ConnectionContext>, IPipe<ConnectionContext>>? _connectionPipeBuilder;

    /// <summary>Initializes a client-context factory.</summary>
    /// <param name="connectionContextSupervisor">The supervisor that supplies the active connection context.</param>
    public ClientContextFactory(IConnectionContextSupervisor connectionContextSupervisor)
        : this(connectionContextSupervisor, null)
    {
    }

    internal ClientContextFactory(IConnectionContextSupervisor connectionContextSupervisor,
        Func<IPipe<ConnectionContext>, IPipe<ConnectionContext>>? connectionPipeBuilder)
    {
        _connectionContextSupervisor = connectionContextSupervisor;
        _connectionPipeBuilder = connectionPipeBuilder;
    }

    /// <summary>Creates an asynchronously established client-context agent.</summary>
    /// <param name="supervisor">The supervisor that owns the agent.</param>
    /// <returns>The new client-context agent.</returns>
    public IPipeContextAgent<ClientContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ClientContext> asyncContext = supervisor.AddAsyncContext<ClientContext>();

        CreateClientContext(asyncContext, supervisor.Stopped);

        return asyncContext;
    }

    /// <summary>Creates an operation-scoped client context from a shared context handle.</summary>
    /// <param name="supervisor">The supervisor that owns the active context.</param>
    /// <param name="context">The shared client-context handle.</param>
    /// <param name="cancellationToken">The operation cancellation token assigned to the scoped context.</param>
    /// <returns>The active scoped-context agent.</returns>
    public IActivePipeContextAgent<ClientContext> CreateActiveContext(ISupervisor supervisor,
        IPipeContextHandle<ClientContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedClientContextAsync(context.Context, cancellationToken));
    }

    static async Task<ClientContext> CreateSharedClientContextAsync(Task<ClientContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully
            ? new ScopeClientContext(context.Result, cancellationToken)
            : new ScopeClientContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    void CreateClientContext(IAsyncPipeContextAgent<ClientContext> asyncContext, CancellationToken cancellationToken)
    {
        Task<ClientContext> CreateAsync(ConnectionContext connectionContext, CancellationToken createCancellationToken)
        {
            return _connectionPipeBuilder is null
                ? Task.FromResult(connectionContext.CreateClientContext(createCancellationToken))
                : CreateFilteredClientContextAsync(connectionContext, createCancellationToken);
        }

        _connectionContextSupervisor.StartAgent(asyncContext, CreateAsync, cancellationToken);
    }
    async Task<ClientContext> CreateFilteredClientContextAsync(ConnectionContext context, CancellationToken cancellationToken)
    {
        var terminal = new ClientCreationPipe(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            IPipe<ConnectionContext> pipe = _connectionPipeBuilder!(terminal)
                ?? throw new InvalidOperationException("The configured connection pipeline returned no pipe.");
            await pipe.SendAsync(context).ConfigureAwait(false);
            return terminal.Created
                ?? throw new InvalidOperationException("The configured connection pipeline completed without creating a client context.");
        }
        catch (Exception operationFailure)
        {
            if (terminal.Created is not null)
            {
                try
                {
                    await PipeContextDisposer.DisposeAsync(terminal.Created).ConfigureAwait(false);
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException("Connection middleware and unpublished client cleanup both failed.",
                        operationFailure, cleanupFailure);
                }
            }

            throw;
        }
    }

    sealed class ClientCreationPipe : IPipe<ConnectionContext>
    {
        readonly CancellationToken _cancellationToken;
        int _calls;

        public ClientCreationPipe(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public ClientContext? Created { get; private set; }

        public Task SendAsync(ConnectionContext context)
        {
            if (Interlocked.Exchange(ref _calls, 1) != 0)
                throw new InvalidOperationException("The configured connection pipeline invoked client creation more than once.");

            _cancellationToken.ThrowIfCancellationRequested();
            Created = context.CreateClientContext(_cancellationToken)
                ?? throw new InvalidOperationException("The connection context returned no client context.");
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateFilterScope("createClientContext");
        }
    }

}
