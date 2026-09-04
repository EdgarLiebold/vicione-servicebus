using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AzureServiceBus.Middleware;

/// <summary>
/// Provides a set session id filter implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class SetSessionIdFilter<T> :
    IFilter<ServiceBusSendContext<T>>
    where T : class
{
    readonly IMessageSessionIdFormatter<T> _sessionIdFormatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sessionIdFormatter">The session id formatter value.</param>
    public SetSessionIdFilter(IMessageSessionIdFormatter<T> sessionIdFormatter)
    {
        _sessionIdFormatter = sessionIdFormatter;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="next">The next value.</param>
    /// <returns>The result of the operation.</returns>
    public Task SendAsync(ServiceBusSendContext<T> context, IPipe<ServiceBusSendContext<T>> next)
    {
        var sessionId = _sessionIdFormatter.FormatSessionId(context);

        if (!string.IsNullOrWhiteSpace(sessionId))
            context.SessionId = sessionId;

        return next.SendAsync(context);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("setSessionId");
    }
}
