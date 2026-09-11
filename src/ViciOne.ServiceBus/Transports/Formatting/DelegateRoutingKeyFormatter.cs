using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Formats routing keys by invoking a message-specific delegate.</summary>
/// <typeparam name="TMessage">The message contract whose send context is formatted.</typeparam>
public sealed class DelegateRoutingKeyFormatter<TMessage> :
    IMessageRoutingKeyFormatter<TMessage>
    where TMessage : class
{
    readonly Func<SendContext<TMessage>, string> _formatter;

    /// <summary>Initializes the formatter with the delegate used for each message.</summary>
    /// <param name="formatter">The delegate that produces a non-null routing key.</param>
    public DelegateRoutingKeyFormatter(Func<SendContext<TMessage>, string> formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>Formats the routing key for a message send.</summary>
    /// <param name="context">The message send context.</param>
    /// <returns>The routing key produced by the configured delegate.</returns>
    public string FormatRoutingKey(SendContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _formatter(context)
            ?? throw new InvalidOperationException("The routing-key formatter returned null.");
    }
}
