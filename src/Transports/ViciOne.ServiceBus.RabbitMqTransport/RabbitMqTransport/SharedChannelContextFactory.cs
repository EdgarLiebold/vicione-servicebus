using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.RabbitMqTransport;

public class SharedChannelContextFactory :
    IPipeContextFactory<ChannelContext>
{
    readonly IChannelContextSupervisor _supervisor;

    public SharedChannelContextFactory(IChannelContextSupervisor supervisor)
    {
        _supervisor = supervisor;
    }

    public IPipeContextAgent<ChannelContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ChannelContext> asyncContext = supervisor.AddAsyncContext<ChannelContext>();

        Task<ChannelContext> context = CreateChannel(asyncContext, supervisor.Stopped);

        async Task HandleShutdown(object sender, ShutdownEventArgs args)
        {
            if (args.Initiator != ShutdownInitiator.Application)
                await asyncContext.Stop(args.ReplyText).ConfigureAwait(false);
        }

        context.GetAwaiter().OnCompleted(() =>
        {
            if (!context.IsCompletedSuccessfully)
                return;

            ChannelContext channelContext = context.Result;
            channelContext.Channel.ChannelShutdownAsync += HandleShutdown;

            asyncContext.Completed.GetAwaiter().OnCompleted(() =>
                channelContext.Channel.ChannelShutdownAsync -= HandleShutdown);
        });

        return asyncContext;
    }

    public IActivePipeContextAgent<ChannelContext> CreateActiveContext(ISupervisor supervisor,
        PipeContextHandle<ChannelContext> context, CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedChannel(context.Context, cancellationToken));
    }

    static async Task<ChannelContext> CreateSharedChannel(Task<ChannelContext> context, CancellationToken cancellationToken)
    {
        return context.IsCompletedSuccessfully()
            ? new SharedChannelContext(context.Result, cancellationToken)
            : new SharedChannelContext(await context.OrCanceled(cancellationToken).ConfigureAwait(false), cancellationToken);
    }

    Task<ChannelContext> CreateChannel(IAsyncPipeContextAgent<ChannelContext> asyncContext, CancellationToken cancellationToken)
    {
        static Task<ChannelContext> CreateChannelContext(ChannelContext context, CancellationToken createCancellationToken)
        {
            return Task.FromResult<ChannelContext>(new SharedChannelContext(context, createCancellationToken));
        }

        return _supervisor.CreateAgent(asyncContext, CreateChannelContext, cancellationToken);
    }
}
