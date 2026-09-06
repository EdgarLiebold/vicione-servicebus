using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>Assigns a formatted Azure Service Bus session identifier before continuing the send pipeline.</summary>
/// <typeparam name="T">The message type being formatted.</typeparam>
public class SetSessionIdFilter<T> :
    IFilter<ServiceBusSendContext<T>>
    where T : class
{
    readonly IMessageSessionIdFormatter<T> _sessionIdFormatter;

    /// <summary>Initializes the filter with a message-specific session-id formatter.</summary>
    /// <param name="sessionIdFormatter">The formatter invoked for each send context.</param>
    public SetSessionIdFilter(IMessageSessionIdFormatter<T> sessionIdFormatter)
    {
        _sessionIdFormatter = sessionIdFormatter;
    }

    /// <summary>Sets a non-empty formatted session identifier and continues the pipeline.</summary>
    /// <param name="context">The Azure Service Bus send context to update.</param>
    /// <param name="next">The remaining provider-specific pipeline.</param>
    /// <returns>The continuation task after any non-empty formatted session identifier has been applied.</returns>
    public Task SendAsync(ServiceBusSendContext<T> context, IPipe<ServiceBusSendContext<T>> next)
    {
        var sessionId = _sessionIdFormatter.FormatSessionId(context);

        if (!string.IsNullOrWhiteSpace(sessionId))
            context.SessionId = sessionId;

        return next.SendAsync(context);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The probe context that receives the filter scope.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("setSessionId");
    }
}
