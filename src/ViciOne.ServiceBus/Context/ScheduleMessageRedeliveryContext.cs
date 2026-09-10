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

    /// <summary>Schedules the consumed message for redelivery to its receive endpoint.</summary>
    /// <param name="delay">The delay before the message becomes due for redelivery.</param>
    /// <param name="callback">An optional action that customizes the scheduled send context.</param>
    /// <param name="cancellationToken">Cancels scheduling before the message is accepted.</param>
    /// <returns>A task that completes when the scheduler accepts the redelivery.</returns>
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
