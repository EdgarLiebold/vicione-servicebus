using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using ViciOne.ServiceBus.RabbitMqTransport.Middleware;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMqTransport;

public class RabbitMqMoveTransport<TSettings>
    where TSettings : class
{
    readonly string _exchange;
    readonly ConfigureRabbitMqTopologyFilter<TSettings> _topologyFilter;

    protected RabbitMqMoveTransport(string exchange, ConfigureRabbitMqTopologyFilter<TSettings> topologyFilter)
    {
        _topologyFilter = topologyFilter;
        _exchange = exchange;
    }

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
