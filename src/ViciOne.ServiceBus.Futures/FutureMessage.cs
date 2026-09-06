using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Stores serialized message properties together with the contract URNs required to deserialize them.</summary>
public sealed class FutureMessage
{
    IDictionary<string, object> _message = new Dictionary<string, object>();
    string[] _supportedMessageTypes = [];

    /// <summary>Creates an empty instance for persistence materialization.</summary>
    public FutureMessage()
    {
    }

    /// <summary>Creates a stored message from its property values and supported contract URNs.</summary>
    /// <param name="message">The serialized message property values.</param>
    /// <param name="supportedMessageTypes">The URNs of contracts represented by the message.</param>
    public FutureMessage(IDictionary<string, object> message, string[] supportedMessageTypes)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(supportedMessageTypes);
        Message = message;
        SupportedMessageTypes = supportedMessageTypes;
    }

    /// <summary>Gets or sets the serialized message property values.</summary>
    public IDictionary<string, object> Message
    {
        get => _message;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _message = value;
        }
    }

    /// <summary>Gets or sets the URNs of contracts represented by the message.</summary>
    public string[] SupportedMessageTypes
    {
        get => _supportedMessageTypes;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _supportedMessageTypes = value;
        }
    }

    /// <summary>Determines whether the stored message represents the specified contract.</summary>
    /// <param name="messageType">The message contract to locate.</param>
    /// <returns><see langword="true" /> when the contract is supported; otherwise, <see langword="false" />.</returns>
    public bool HasMessageType(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        var typeUrn = MessageUrn.ForTypeString(messageType);

        return SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Determines whether the stored message represents the specified contract.</summary>
    /// <typeparam name="T">The message contract to locate.</typeparam>
    /// <returns><see langword="true" /> when the contract is supported; otherwise, <see langword="false" />.</returns>
    public bool HasMessageType<T>()
        where T : class
    {
        var typeUrn = MessageUrn.ForTypeString<T>();

        return SupportedMessageTypes.Any(x => typeUrn.Equals(x, StringComparison.OrdinalIgnoreCase));
    }
}
