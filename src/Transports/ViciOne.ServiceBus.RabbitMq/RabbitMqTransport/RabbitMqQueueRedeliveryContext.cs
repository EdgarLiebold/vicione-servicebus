using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Middleware;
using ViciOne.ServiceBus.Serialization;

#nullable enable
namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a rabbit mq queue redelivery context implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public sealed class RabbitMqQueueRedeliveryContext<TMessage> : MessageRedeliveryContext
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly RedeliveryOptions _options;
    readonly RabbitMqQueueRedeliveryPlan _plan;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="options">The options value.</param>
    /// <param name="plan">The plan value.</param>
    public RabbitMqQueueRedeliveryContext(ConsumeContext<TMessage> context, RedeliveryOptions options, RabbitMqQueueRedeliveryPlan plan)
    {
        _context = context;
        _options = options;
        _plan = plan;
    }

    /// <summary>
    /// Schedules redelivery.
    /// </summary>
    /// <param name="delay">The delay value.</param>
    /// <param name="callback">The callback value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
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
