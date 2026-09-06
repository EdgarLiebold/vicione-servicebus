using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a delayed message redelivery context implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class DelayedMessageRedeliveryContext<TMessage> :
    MessageRedeliveryContext
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly RedeliveryOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="options">The options value.</param>
    public DelayedMessageRedeliveryContext(ConsumeContext<TMessage> context, RedeliveryOptions options)
    {
        _context = context;
        _options = options;
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
