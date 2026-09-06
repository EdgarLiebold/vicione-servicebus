using System.Diagnostics;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Adds the scheduler to the consume context, so that it can be used for message redelivery.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class ScheduleMessageRedeliveryFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RedeliveryOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    public ScheduleMessageRedeliveryFilter(RedeliveryOptions options)
    {
        _options = options;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("scheduleRedeliveryContext");
    }

    /// <summary>Sends a message to the configured destination.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        context.GetOrAddPayload<MessageRedeliveryContext>(() => new ScheduleMessageRedeliveryContext<TMessage>(context, _options));

        return next.SendAsync(context);
    }
}
