using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Creates supervised shared-channel leases and stops them when RabbitMQ closes the underlying channel.</summary>
public class SharedChannelContextFactory :
    IPipeContextFactory<ChannelContext>
{
    readonly IChannelContextSupervisor _supervisor;

    /// <summary>Creates a factory backed by a channel-context supervisor.</summary>
    /// <param name="supervisor">The supervisor that establishes the underlying channel contexts.</param>
    public SharedChannelContextFactory(IChannelContextSupervisor supervisor)
    {
        _supervisor = supervisor;
    }

    /// <summary>Creates and supervises a shared channel context.</summary>
    /// <param name="supervisor">The lifetime supervisor that owns the returned context agent.</param>
    /// <returns>An asynchronous agent whose context becomes available when the channel has been established.</returns>
    public IPipeContextAgent<ChannelContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ChannelContext> asyncContext = supervisor.AddAsyncContext<ChannelContext>();

        Task<ChannelContext> context = CreateChannelAsync(asyncContext, supervisor.Stopped);

        async Task HandleShutdownAsync(object sender, ShutdownEventArgs args)
        {
            if (args.Initiator != ShutdownInitiator.Application)
                await asyncContext.StopAsync(args.ReplyText).ConfigureAwait(false);
        }

        context.GetAwaiter().OnCompleted(() =>
        {
            if (!context.IsCompletedSuccessfully)
                return;

            ChannelContext channelContext = context.Result;
            channelContext.Channel.ChannelShutdownAsync += HandleShutdownAsync;

            asyncContext.Completed.GetAwaiter().OnCompleted(() =>
                channelContext.Channel.ChannelShutdownAsync -= HandleShutdownAsync);
        });

        return asyncContext;
    }

    /// <summary>Creates an active shared-channel lease over an existing context handle.</summary>
    /// <param name="supervisor">The lifetime supervisor that owns the active lease.</param>
    /// <param name="context">The handle that supplies the underlying channel context.</param>
    /// <param name="cancellationToken">The token that ends the active lease.</param>
    /// <returns>An active agent for the shared channel context.</returns>
    public IActivePipeContextAgent<ChannelContext> CreateActiveContext(ISupervisor supervisor,
        IPipeContextHandle<ChannelContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedChannelAsync(context.Context, cancellationToken));
    }

    static async Task<ChannelContext> CreateSharedChannelAsync(Task<ChannelContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedChannelContext(context.Result, cancellationToken)
            : new SharedChannelContext(await context.OrCanceledAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    Task<ChannelContext> CreateChannelAsync(IAsyncPipeContextAgent<ChannelContext> asyncContext, CancellationToken cancellationToken)
    {
        static Task<ChannelContext> CreateChannelContextAsync(ChannelContext context, CancellationToken createCancellationToken)
        {
            return Task.FromResult<ChannelContext>(new SharedChannelContext(context, createCancellationToken));
        }

        return _supervisor.CreateAgentAsync(asyncContext, CreateChannelContextAsync, cancellationToken);
    }
}
