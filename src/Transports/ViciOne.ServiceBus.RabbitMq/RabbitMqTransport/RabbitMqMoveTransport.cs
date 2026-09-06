using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMq.Middleware;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq move transport implementation.
/// </summary>
/// <typeparam name="TSettings">The t settings type.</typeparam>
public class RabbitMqMoveTransport<TSettings>
    where TSettings : class
{
    readonly string _exchange;
    readonly ConfigureRabbitMqTopologyFilter<TSettings> _topologyFilter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="exchange">The exchange value.</param>
    /// <param name="topologyFilter">The topology filter value.</param>
    protected RabbitMqMoveTransport(string exchange, ConfigureRabbitMqTopologyFilter<TSettings> topologyFilter)
    {
        _topologyFilter = topologyFilter;
        _exchange = exchange;
    }

    /// <summary>
    /// Performs the move operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="preSend">The pre send value.</param>
    /// <returns>The result of the operation.</returns>
    protected async Task MoveAsync(ReceiveContext context, Action<BasicProperties, SendHeaders> preSend)
    {
        if (!context.TryGetPayload(out ChannelContext? channelContext))
            throw new ArgumentException("The ReceiveContext must contain a ChannelContext", nameof(context));

        if (channelContext.Channel.IsClosed)
        {
            // Channel.IsClosed and Channel.CloseReason are read-only diagnostics and safe to read at
            // any time; the operations themselves go through the owning context and its lease. The
            // reason reported is the one the channel actually closed for. Where there is none — a
            // close this process started — the initiator is Library, because a locally produced
            // state must not claim the peer sent it.
            var reason = channelContext.Channel.CloseReason;

            throw new OperationInterruptedException(reason
                ?? new ShutdownEventArgs(ShutdownInitiator.Library, 491, "The channel is no longer available"));
        }

        OneTimeContext<ConfigureTopologyContext<TSettings>> oneTimeContext =
            await _topologyFilter.ConfigureAsync(channelContext, context.CancellationToken).ConfigureAwait(false);

        BasicProperties properties;
        var routingKey = "";
        byte[] body;

        if (context.TryGetPayload(out RabbitMqBasicConsumeContext? basicConsumeContext))
        {
            properties = new BasicProperties(basicConsumeContext.Properties);
            routingKey = basicConsumeContext.RoutingKey!;
            body = context.GetBody();
        }
        else
        {
            properties = new BasicProperties { Headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) };
            body = context.GetBody();
        }

        SendHeaders headers = new MoveTransportHeaders(properties);

        headers.SetHostHeaders();

        preSend(properties, headers);

        try
        {
            await channelContext.BasicPublishAsync(_exchange, routingKey, true, properties, body, true, context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            oneTimeContext?.Evict();
            throw;
        }
    }
}
