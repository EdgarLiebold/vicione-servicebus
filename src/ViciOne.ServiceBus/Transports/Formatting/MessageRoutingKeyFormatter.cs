using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Adapts a non-generic routing-key formatter to a message-specific contract.</summary>
/// <typeparam name="TMessage">The message contract whose send context is formatted.</typeparam>
public sealed class MessageRoutingKeyFormatter<TMessage> :
    IMessageRoutingKeyFormatter<TMessage>
    where TMessage : class
{
    readonly IRoutingKeyFormatter _formatter;

    /// <summary>Initializes the adapter with its underlying formatter.</summary>
    /// <param name="formatter">The formatter that handles arbitrary message contracts.</param>
    public MessageRoutingKeyFormatter(IRoutingKeyFormatter formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>Formats the routing key for a message send.</summary>
    /// <param name="context">The message send context.</param>
    /// <returns>The routing key produced by the underlying formatter.</returns>
    public string FormatRoutingKey(SendContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _formatter.FormatRoutingKey(context)
            ?? throw new InvalidOperationException("The routing-key formatter returned null.");
    }
}
