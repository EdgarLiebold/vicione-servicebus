using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Agents;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a channel context factory implementation.
/// </summary>
public class ChannelContextFactory :
    IPipeContextFactory<ChannelContext>
{
    readonly ushort? _concurrentMessageLimit;
    readonly IConnectionContextSupervisor _supervisor;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="supervisor">The supervisor value.</param>
    /// <param name="concurrentMessageLimit">The concurrent message limit value.</param>
    public ChannelContextFactory(IConnectionContextSupervisor supervisor, ushort? concurrentMessageLimit)
    {
        _supervisor = supervisor;
        _concurrentMessageLimit = concurrentMessageLimit;
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

        Task HandleShutdownAsync(object sender, ShutdownEventArgs args)
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

            return asyncContext.StopAsync(args.ReplyText);
        }

        context.GetAwaiter().OnCompleted(() =>
        {
            if (!context.IsCompletedSuccessfully)
                return;

            var channelContext = context.Result;

            channelContext.Channel.ChannelShutdownAsync += HandleShutdownAsync;
            channelContext.ConnectionContext.Connection.ConnectionShutdownAsync += HandleShutdownAsync;

            void RemoveHandlers()
            {
                try
                {
                    channelContext.ConnectionContext.Connection.ConnectionShutdownAsync -= HandleShutdownAsync;
                }
                catch (ObjectDisposedException)
                {
                }

                try
                {
                    channelContext.Channel.ChannelShutdownAsync -= HandleShutdownAsync;
                }
                catch (ObjectDisposedException)
                {
                }
            }

            asyncContext.Completed.GetAwaiter().OnCompleted(RemoveHandlers);
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
    public IActivePipeContextAgent<ChannelContext> CreateActiveContext(ISupervisor supervisor, PipeContextHandle<ChannelContext> context,
        CancellationToken cancellationToken)
    {
        return supervisor.AddActiveContext(context, CreateSharedChannelAsync(context.Context, cancellationToken));
    }

    static async Task<ChannelContext> CreateSharedChannelAsync(Task<ChannelContext> contextTask, CancellationToken cancellationToken)
    {
        var context = contextTask.Status == TaskStatus.RanToCompletion
            ? contextTask.Result
            : await contextTask.OrCanceledAsync(cancellationToken).ConfigureAwait(false);

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

    Task<ChannelContext> CreateChannelAsync(IAsyncPipeContextAgent<ChannelContext> asyncContext, CancellationToken cancellationToken)
    {
        Task<ChannelContext> CreateChannelContextAsync(ConnectionContext connectionContext, CancellationToken createCancellationToken,
            ushort? concurrentMessageLimit)
        {
            return connectionContext.CreateChannelContextAsync(asyncContext, concurrentMessageLimit, createCancellationToken);
        }

        return _supervisor.CreateAgentAsync(asyncContext, (context, token) => CreateChannelContextAsync(context, token, _concurrentMessageLimit), cancellationToken);
    }
}
