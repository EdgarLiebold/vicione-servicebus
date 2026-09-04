using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Adds the scheduler to the consume context, so that it can be used for message redelivery
/// </summary>
public class ScheduleMessageRedeliveryFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RedeliveryOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public ScheduleMessageRedeliveryFilter(RedeliveryOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scheduleRedeliveryContext");
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        context.GetOrAddPayload<MessageRedeliveryContext>(() => new ScheduleMessageRedeliveryContext<TMessage>(context, _options));

        return next.SendAsync(context);
    }
}
