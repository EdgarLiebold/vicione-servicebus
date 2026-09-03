namespace ViciOne.ServiceBus.RabbitMqTransport;

using System;
using System.Threading;
using System.Threading.Tasks;
using Agents;
using Internals;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;


public class ChannelContextFactory :
    IPipeContextFactory<ChannelContext>
{
    readonly ushort? _concurrentMessageLimit;
    readonly IConnectionContextSupervisor _supervisor;

    public ChannelContextFactory(IConnectionContextSupervisor supervisor, ushort? concurrentMessageLimit)
    {
        _supervisor = supervisor;
        _concurrentMessageLimit = concurrentMessageLimit;
    }

    public IPipeContextAgent<ChannelContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ChannelContext> asyncContext = supervisor.AddAsyncContext<ChannelContext>();

        Task<ChannelContext> context = CreateChannel(asyncContext, supervisor.Stopped);

        Task HandleShutdown(object sender, ShutdownEventArgs args)
        {
            // Invalidation first, and it keeps the broker's own reason. Disposal is not started here:
            // the client raises this notification while the refused operation is still unwinding, and
            // taking the channel away underneath it is what replaced the broker's answer with an
            // ObjectDisposedException. The lifetime disposes once the last operation has finished.
            if (context.Status == TaskStatus.RanToCompletion && context.Result is RabbitMqChannelContext channelContext)
            {
                channelContext.ConnectionContext.TopologyEntityCache.Invalidate();
                channelContext.Lifetime.Invalidate(args);
            }

            Task.Run(() => asyncContext.Stop(args.ReplyText))
                .IgnoreUnobservedExceptions();

            return Task.CompletedTask;
        }

        context.ContinueWith(task =>
        {
            var channelContext = task.Result;

            channelContext.Channel.ChannelShutdownAsync += HandleShutdown;
            channelContext.ConnectionContext.Connection.ConnectionShutdownAsync += HandleShutdown;

            void RemoveHandlers()
            {
                try
                {
                    channelContext.ConnectionContext.Connection.ConnectionShutdownAsync -= HandleShutdown;
                }
                catch (ObjectDisposedException)
                {
                }

                try
                {
                    channelContext.Channel.ChannelShutdownAsync -= HandleShutdown;
                }
                catch (ObjectDisposedException)
                {
                }
            }

            asyncContext.Completed.ContinueWith(_ => RemoveHandlers());
        }, TaskContinuationOptions.OnlyOnRanToCompletion);

        return asyncContext;
    }

    public IActivePipeContextAgent<ChannelContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<ChannelContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedChannel(context.Context, cancellationToken));
    }

    static async Task<ChannelContext> CreateSharedChannel(Task<ChannelContext> contextTask, CancellationToken cancellationToken)
    {
        var context = contextTask.Status == TaskStatus.RanToCompletion
            ? contextTask.Result
            : await contextTask.OrCanceled(cancellationToken).ConfigureAwait(false);

        if (context.Channel.IsClosed)
        {
            var reason = context.Channel.CloseReason;

            // The broker's own answer when there is one, and this transport's own when there is not.
            // Nothing is invented, and nothing travels as prose or in Exception.Data.
            throw reason != null
                ? new OperationInterruptedException(reason)
                : new OperationInterruptedException(
                    new ShutdownEventArgs(ShutdownInitiator.Library, 491, "The channel is no longer available"));
        }

        return new ScopeChannelContext(context, cancellationToken);
    }

    Task<ChannelContext> CreateChannel(IAsyncPipeContextAgent<ChannelContext> asyncContext, CancellationToken cancellationToken)
    {
        Task<ChannelContext> CreateChannelContext(ConnectionContext connectionContext, CancellationToken createCancellationToken,
            ushort? concurrentMessageLimit)
        {
            return connectionContext.CreateChannelContext(asyncContext, concurrentMessageLimit, createCancellationToken);
        }

        return _supervisor.CreateAgent(asyncContext, (context, token) => CreateChannelContext(context, token, _concurrentMessageLimit), cancellationToken);
    }
}
