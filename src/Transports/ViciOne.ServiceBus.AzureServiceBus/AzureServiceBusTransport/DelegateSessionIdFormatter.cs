using System;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Derives Azure Service Bus session identifiers with a caller-supplied delegate.</summary>
/// <typeparam name="TMessage">The sent message contract.</typeparam>
public class DelegateSessionIdFormatter<TMessage> :
    IMessageSessionIdFormatter<TMessage>
    where TMessage : class
{
    readonly Func<SendContext<TMessage>, string> _formatter;

    /// <summary>Creates a formatter backed by a delegate.</summary>
    /// <param name="formatter">The delegate that derives a session identifier from the send context.</param>
    public DelegateSessionIdFormatter(Func<SendContext<TMessage>, string> formatter)
    {
        _formatter = formatter;
    }

    /// <summary>Formats the session identifier, converting a <see langword="null"/> delegate result to an empty string.</summary>
    /// <param name="context">The typed send context.</param>
    /// <returns>The derived session identifier.</returns>
    public string FormatSessionId(SendContext<TMessage> context)
    {
        return _formatter(context) ?? "";
    }
}
