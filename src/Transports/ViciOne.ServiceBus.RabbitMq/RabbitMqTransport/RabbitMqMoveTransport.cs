using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMq.Middleware;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Copies received messages to a RabbitMQ exchange after ensuring its topology exists.</summary>
/// <typeparam name="TSettings">The destination topology settings.</typeparam>
public class RabbitMqMoveTransport<TSettings>
    where TSettings : class
{
    readonly string _exchange;
    readonly ConfigureRabbitMqTopologyFilter<TSettings> _topologyFilter;

    /// <summary>Creates a move transport for one destination exchange and topology plan.</summary>
    /// <param name="exchange">The destination exchange name.</param>
    /// <param name="topologyFilter">The filter that declares destination topology.</param>
    protected RabbitMqMoveTransport(string exchange, ConfigureRabbitMqTopologyFilter<TSettings> topologyFilter)
    {
        _topologyFilter = topologyFilter;
        _exchange = exchange;
    }

    /// <summary>Copies the receive body and AMQP properties, customizes them, and publishes to the destination exchange.</summary>
    /// <param name="context">The received message and active RabbitMQ channel.</param>
    /// <param name="preSend">The callback that adds move-specific properties and headers.</param>
    /// <returns>A task that follows the mandatory RabbitMQ client publish operation; source settlement remains the receive pipeline's responsibility.</returns>
    protected async Task MoveAsync(ReceiveContext context, Action<BasicProperties, SendHeaders> preSend)
    {
        if (!context.TryGetPayload(out ChannelContext? channelContext))
            throw new ArgumentException("The ReceiveContext must contain a ChannelContext", nameof(context));

        if (channelContext.Channel.IsClosed)
        {
            // Preserve a broker close reason when available; otherwise identify the synthesized
            // unavailable state as library-initiated.
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
            body = context.GetBodyContent();
        }
        else
        {
            properties = new BasicProperties { Headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) };
            body = context.GetBodyContent();
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
