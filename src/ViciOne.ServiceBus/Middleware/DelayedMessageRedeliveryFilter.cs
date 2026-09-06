using System.Diagnostics;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Adds the transport-neutral delayed-redelivery context used to schedule a message retry.
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public class DelayedMessageRedeliveryFilter<TMessage> :
    IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    readonly RedeliveryOptions _options;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="options">The options value.</param>
    public DelayedMessageRedeliveryFilter(RedeliveryOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        var scope = context.CreateFilterScope("delayedMessageRedelivery");
        scope.Add("messageType", TypeCache<TMessage>.ShortName);
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
        context.GetOrAddPayload<MessageRedeliveryContext>(() => new DelayedMessageRedeliveryContext<TMessage>(context, _options));

        return next.SendAsync(context);
    }
}
