using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a shared channel context factory implementation.
/// </summary>
public class SharedChannelContextFactory :
    IPipeContextFactory<ChannelContext>
{
    readonly IChannelContextSupervisor _supervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    public SharedChannelContextFactory(IChannelContextSupervisor supervisor)
    {
        _supervisor = supervisor;
    }

    /// <summary>
    /// Creates context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Creates active context.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public IActivePipeContextAgent<ChannelContext> CreateActiveContext(ISupervisor supervisor,
        PipeContextHandle<ChannelContext> context, CancellationToken cancellationToken)
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
