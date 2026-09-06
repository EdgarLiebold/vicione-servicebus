using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Republishes a consumed message to a predeclared RabbitMQ TTL redelivery queue.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public sealed class RabbitMqQueueRedeliveryContext<TMessage> : MessageRedeliveryContext
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly RedeliveryOptions _options;
    readonly RabbitMqQueueRedeliveryPlan _plan;

    /// <summary>Creates a redelivery context over the current consume context and finite delay plan.</summary>
    /// <param name="context">The consumed message to redeliver.</param>
    /// <param name="options">The provider-neutral redelivery options.</param>
    /// <param name="plan">The predeclared RabbitMQ delay queues and routing keys.</param>
    public RabbitMqQueueRedeliveryContext(ConsumeContext<TMessage> context, RedeliveryOptions options, RabbitMqQueueRedeliveryPlan plan)
    {
        _context = context;
        _options = options;
        _plan = plan;
    }

    /// <summary>Publishes the message to the TTL queue declared for an exact configured delay.</summary>
    /// <param name="delay">A delay present in the endpoint's redelivery plan.</param>
    /// <param name="callback">An optional callback that customizes the redelivery send context.</param>
    /// <param name="cancellationToken">Cancellation for send-endpoint lookup; the consume context governs topology and publish operations.</param>
    /// <returns>A task that completes after the redelivery publish completes.</returns>
    public async Task ScheduleRedeliveryAsync(TimeSpan delay, Action<ConsumeContext, SendContext>? callback, CancellationToken cancellationToken = default)
    {
        var routingKey = _plan.GetRoutingKey(delay);
        var channel = _context.GetPayload<ChannelContext>();

        await _plan.ConfigureAsync(channel, _context.CancellationToken).ConfigureAwait(false);

        var address = channel.ConnectionContext.Topology.GetDestinationAddress(_plan.DelayExchangeName, exchange =>
        {
            exchange.ExchangeType = ExchangeType.Direct;
            exchange.Durable = _plan.Durable;
            exchange.AutoDelete = _plan.AutoDelete;
        });

        var endpoint = await _context.Advanced().GetSendEndpointAsync(address, cancellationToken).ConfigureAwait(false);

        IPipe<SendContext<TMessage>> pipe = Pipe.Execute<SendContext<TMessage>>(sendContext =>
        {
            sendContext.ApplyRedeliveryOptions(_context.Advanced(), _options);
            sendContext.SetRoutingKey(routingKey);
            if (!string.IsNullOrEmpty(_context.Advanced().RoutingKey()))
                sendContext.Headers.Set(RabbitMqHeaders.RedeliveryRoutingKey, _context.Advanced().RoutingKey());

            callback?.Invoke(_context.Advanced(), sendContext);
        });

        var messagePipe = new ForwardMessagePipe<TMessage>(_context, pipe);
        await endpoint.SendAsync(_context.Message, messagePipe, _context.CancellationToken).ConfigureAwait(false);
    }
}
