using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Adds the transport-neutral delayed-redelivery context used to schedule a message retry.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class DelayedMessageRedeliveryFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RedeliveryOptions _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="options">The options that control the operation.</param>
    public DelayedMessageRedeliveryFilter(RedeliveryOptions options)
    {
        _options = options;
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("delayedMessageRedelivery");
        scope.Add("messageType", TypeCache<TMessage>.ShortName);
    }

    /// <summary>Provides delayed-redelivery context when absent, then forwards the consumed message.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="next">The next pipeline stage to invoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [DebuggerNonUserCode]
    public Task SendAsync(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        context.GetOrAddPayload<MessageRedeliveryContext>(() => new DelayedMessageRedeliveryContext<TMessage>(context, _options));

        return next.SendAsync(context);
    }
}
