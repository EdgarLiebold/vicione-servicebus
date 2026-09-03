#nullable enable
namespace ViciOne.ServiceBus.RabbitMqTransport;

using System;
using System.Threading.Tasks;
using Middleware;
using RabbitMQ.Client;
using Serialization;


public sealed class RabbitMqQueueRedeliveryContext<TMessage> : MessageRedeliveryContext
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly RedeliveryOptions _options;
    readonly RabbitMqQueueRedeliveryPlan _plan;

    public RabbitMqQueueRedeliveryContext(ConsumeContext<TMessage> context, RedeliveryOptions options, RabbitMqQueueRedeliveryPlan plan)
    {
        _context = context;
        _options = options;
        _plan = plan;
    }

    public async Task ScheduleRedelivery(TimeSpan delay, Action<ConsumeContext, SendContext>? callback)
    {
        var routingKey = _plan.GetRoutingKey(delay);
        var channel = _context.GetPayload<ChannelContext>();

        await _plan.Configure(channel, _context.CancellationToken).ConfigureAwait(false);

        var address = channel.ConnectionContext.Topology.GetDestinationAddress(_plan.DelayExchangeName, exchange =>
        {
            exchange.ExchangeType = ExchangeType.Direct;
            exchange.Durable = _plan.Durable;
            exchange.AutoDelete = _plan.AutoDelete;
        });

        var endpoint = await _context.GetSendEndpoint(address).ConfigureAwait(false);

        IPipe<SendContext<TMessage>> pipe = Pipe.Execute<SendContext<TMessage>>(sendContext =>
        {
            sendContext.ApplyRedeliveryOptions(_context, _options);
            sendContext.SetRoutingKey(routingKey);
            if (!string.IsNullOrEmpty(_context.RoutingKey()))
                sendContext.Headers.Set(RabbitMqHeaders.RedeliveryRoutingKey, _context.RoutingKey());

            callback?.Invoke(_context, sendContext);
        });

        var messagePipe = new ForwardMessagePipe<TMessage>(_context, pipe);
        await endpoint.Send(_context.Message, messagePipe, _context.CancellationToken).ConfigureAwait(false);
    }
}
