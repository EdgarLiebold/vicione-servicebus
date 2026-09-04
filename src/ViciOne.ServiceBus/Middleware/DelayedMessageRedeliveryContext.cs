using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Serialization;

#nullable enable
namespace ViciOne.ServiceBus.Middleware;

public class DelayedMessageRedeliveryContext<TMessage> :
    MessageRedeliveryContext
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly RedeliveryOptions _options;

    public DelayedMessageRedeliveryContext(ConsumeContext<TMessage> context, RedeliveryOptions options)
    {
        _context = context;
        _options = options;
    }

    public async Task ScheduleRedeliveryAsync(TimeSpan delay, Action<ConsumeContext, SendContext>? callback, CancellationToken cancellationToken = default)
    {
        IPipe<SendContext<TMessage>> pipe = Pipe.Execute<SendContext<TMessage>>(sendContext =>
        {
            sendContext.ApplyRedeliveryOptions(_context.Advanced(), _options);

            callback?.Invoke(_context.Advanced(), sendContext);
        });

        IPipe<SendContext<TMessage>> delaySendPipe = new DelaySendPipe<TMessage>(pipe, delay);

        var endpoint = await _context.Advanced().GetSendEndpointAsync(_context.Advanced().ReceiveContext.InputAddress, cancellationToken).ConfigureAwait(false);

        var messagePipe = new ForwardMessagePipe<TMessage>(_context, delaySendPipe);

        await endpoint.SendAsync(_context.Message, messagePipe, _context.CancellationToken).ConfigureAwait(false);
    }
}
