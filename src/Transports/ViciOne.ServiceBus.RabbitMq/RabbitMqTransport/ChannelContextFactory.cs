using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Creates supervised RabbitMQ channel contexts and invalidates them on broker shutdown.</summary>
public class ChannelContextFactory :
    IPipeContextFactory<ChannelContext>
{
    readonly ushort? _concurrentMessageLimit;
    readonly IConnectionContextSupervisor _supervisor;

    /// <summary>Creates a channel factory backed by a supervised RabbitMQ connection.</summary>
    /// <param name="supervisor">The connection-context supervisor used to create channels.</param>
    /// <param name="concurrentMessageLimit">The optional consumer concurrency used to size the channel prefetch.</param>
    public ChannelContextFactory(IConnectionContextSupervisor supervisor, ushort? concurrentMessageLimit)
    {
        _supervisor = supervisor;
        _concurrentMessageLimit = concurrentMessageLimit;
    }

    /// <summary>Creates and monitors an owned RabbitMQ channel context.</summary>
    /// <param name="supervisor">The supervisor that owns the context agent.</param>
    /// <returns>The asynchronous channel-context agent.</returns>
    public IPipeContextAgent<ChannelContext> CreateContext(ISupervisor supervisor)
    {
        IAsyncPipeContextAgent<ChannelContext> asyncContext = supervisor.AddAsyncContext<ChannelContext>();

        Task<ChannelContext> context = CreateChannelAsync(asyncContext, supervisor.Stopped);

        Task HandleShutdownAsync(object sender, ShutdownEventArgs args)
        {
            // Preserve the broker reason and defer disposal until every active channel lease has finished.
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

    /// <summary>Creates a scoped view over an existing active channel context.</summary>
    /// <param name="supervisor">The supervisor that owns the active context.</param>
    /// <param name="context">The handle for the shared channel context.</param>
    /// <param name="cancellationToken">Cancellation linked to the scoped view.</param>
    /// <returns>The active scoped context agent.</returns>
    public IActivePipeContextAgent<ChannelContext> CreateActiveContext(ISupervisor supervisor, IPipeContextHandle<ChannelContext> context,
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

            // Prefer the typed broker reply; synthesize a library reply only when none exists.
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
