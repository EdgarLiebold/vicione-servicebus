using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Context;

/// <summary>Schedules redelivery of a consumed message through the configured message scheduler.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public class ScheduleMessageRedeliveryContext<TMessage> :
    MessageRedeliveryContext
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _context;
    readonly RedeliveryOptions _options;

    /// <summary>Creates a redelivery context for one consumed message.</summary>
    /// <param name="context">The consumed message to redeliver.</param>
    /// <param name="options">The redelivery options applied to the scheduled send.</param>
    public ScheduleMessageRedeliveryContext(ConsumeContext<TMessage> context, RedeliveryOptions options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
