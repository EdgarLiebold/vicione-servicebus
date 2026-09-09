using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Creates connection context instances.</summary>
public abstract class ConnectionContextFactory :
    IPipeContextFactory<ConnectionContext>
{
    /// <summary>Creates context.</summary>
    /// <param name="supervisor">The supervisor.</param>
    /// <returns>The created context.</returns>
    public IPipeContextAgent<ConnectionContext> CreateContext(ISupervisor supervisor)
    {
        ITransportSupervisor<ConnectionContext> transportSupervisor =
            supervisor as ITransportSupervisor<ConnectionContext> ?? throw new ArgumentException(nameof(supervisor));

        return supervisor.AddContext(CreateConnection(transportSupervisor));
    }

    /// <summary>Creates active context.</summary>
    /// <param name="supervisor">The supervisor.</param>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The created active context.</returns>
    public IActivePipeContextAgent<ConnectionContext> CreateActiveContext(ISupervisor supervisor,
        IPipeContextHandle<ConnectionContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedConnectionAsync(context.Context, cancellationToken));
    }

    static async Task<ConnectionContext> CreateSharedConnectionAsync(Task<ConnectionContext> context, CancellationToken cancellationToken)
    {
        return context.Status == TaskStatus.RanToCompletion
            ? new SharedConnectionContext(context.Result, cancellationToken)
            : new SharedConnectionContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    /// <summary>Creates connection.</summary>
    /// <param name="supervisor">The supervisor.</param>
    /// <returns>The created connection.</returns>
    protected abstract ConnectionContext CreateConnection(ITransportSupervisor<ConnectionContext> supervisor);
}
