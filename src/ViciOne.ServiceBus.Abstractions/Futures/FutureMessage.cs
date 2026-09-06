using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Carries future message data.</summary>
public class FutureMessage
{
    /// <summary>Initializes a new instance.</summary>
    public FutureMessage()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="supportedMessageTypes">The supported message types.</param>
    public FutureMessage(IDictionary<string, object> message, string[] supportedMessageTypes)
    {
        Message = message;
        SupportedMessageTypes = supportedMessageTypes;
    }

    /// <summary>Gets or sets the message.</summary>
    public IDictionary<string, object> Message { get; set; } = null!;

    /// <summary>Gets or sets the supported message types.</summary>
    public string[] SupportedMessageTypes { get; set; } = null!;

    /// <summary>Determines whether the current value has message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasMessageType(Type messageType)
    {
        var typeUrn = MessageUrn.ForTypeString(messageType);

        return SupportedMessageTypes?.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase)) ?? false;
    }

    /// <summary>Determines whether the current value has message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool HasMessageType<T>()
        where T : class
    {
        var typeUrn = MessageUrn.ForTypeString<T>();

        return SupportedMessageTypes?.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase)) ?? false;
    }
}
