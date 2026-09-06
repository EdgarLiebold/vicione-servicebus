namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Adapts a runtime-typed session identifier formatter to a message contract.</summary>
/// <typeparam name="TMessage">The sent message contract.</typeparam>
public class MessageSessionIdFormatter<TMessage> :
    IMessageSessionIdFormatter<TMessage>
    where TMessage : class
{
    readonly ISessionIdFormatter _formatter;

    /// <summary>Creates an adapter over a runtime-typed formatter.</summary>
    /// <param name="formatter">The formatter to invoke for typed send contexts.</param>
    public MessageSessionIdFormatter(ISessionIdFormatter formatter)
    {
        _formatter = formatter;
    }

    /// <summary>Formats the session identifier for an outgoing message.</summary>
    /// <param name="context">The typed send context.</param>
    /// <returns>The derived session identifier, or <see langword="null"/> when none applies.</returns>
    public string? FormatSessionId(SendContext<TMessage> context)
    {
        return _formatter.FormatSessionId(context);
    }
}
