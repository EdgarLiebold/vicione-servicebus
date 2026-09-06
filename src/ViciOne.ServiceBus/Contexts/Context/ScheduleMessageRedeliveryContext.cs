using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Context;

/// <summary>Used to schedule message redelivery using the message scheduler.</summary>
/// <typeparam name="TMessage">The message type.</typeparam>
public class ScheduleMessageRedeliveryContext<TMessage> :
    MessageRedeliveryContext
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly RedeliveryOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="options">The options that control the operation.</param>
    public ScheduleMessageRedeliveryContext(ConsumeContext<TMessage> context, RedeliveryOptions options)
    {
        _context = context;
        _options = options;
    }

    /// <summary>Schedules redelivery.</summary>
    /// <param name="delay">The delay before the operation is attempted.</param>
    /// <param name="callback">The callback invoked by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ScheduleRedeliveryAsync(TimeSpan delay, Action<ConsumeContext, SendContext>? callback, CancellationToken cancellationToken = default)
    {
        var schedulerContext = _context.GetPayload<MessageSchedulerContext>();

        void SendCallback(ConsumeContext consumeContext, SendContext sendContext)
        {
            sendContext.ApplyRedeliveryOptions(consumeContext, _options);

            callback?.Invoke(consumeContext, sendContext);
        }

        return schedulerContext.ScheduleSendAsync(delay, _context.Message, new CopyContextPipe(_context.Advanced(), SendCallback),
            cancellationToken: cancellationToken);
    }
}
