using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Futures;

/// <summary>Stores serialized message properties together with the contract URNs required to deserialize them.</summary>
public sealed class FutureMessage
{
    IReadOnlyDictionary<string, object> _message = FrozenDictionary<string, object>.Empty;
    IReadOnlyList<string> _supportedMessageTypes = Array.AsReadOnly(Array.Empty<string>());

    /// <summary>Creates an empty instance for persistence materialization.</summary>
    public FutureMessage()
    {
    }

    /// <summary>Creates a stored message from its property values and supported contract URNs.</summary>
    /// <param name="message">The serialized message property values.</param>
    /// <param name="supportedMessageTypes">The URNs of contracts represented by the message.</param>
    public FutureMessage(IReadOnlyDictionary<string, object> message, IReadOnlyList<string> supportedMessageTypes)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(supportedMessageTypes);
        _message = Snapshot(message);
        _supportedMessageTypes = SnapshotMessageTypes(supportedMessageTypes, nameof(supportedMessageTypes));
    }

    /// <summary>Gets or sets a detached, read-only snapshot of the serialized message property values.</summary>
    public IReadOnlyDictionary<string, object> Message
    {
        get => _message;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _message = Snapshot(value);
        }
    }

    /// <summary>Gets or sets a detached, read-only snapshot of the URNs represented by the message.</summary>
    public IReadOnlyList<string> SupportedMessageTypes
    {
        get => _supportedMessageTypes;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _supportedMessageTypes = SnapshotMessageTypes(value, nameof(value));
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

    static IReadOnlyDictionary<string, object> Snapshot(IReadOnlyDictionary<string, object> message)
    {
        return message.ToFrozenDictionary();
    }

    static IReadOnlyList<string> SnapshotMessageTypes(IReadOnlyList<string> messageTypes, string parameterName)
    {
        string[] snapshot = [.. messageTypes];
        if (snapshot.Any(static value => !IsMessageContractUrn(value)))
            throw new ArgumentException("Message contract identifiers must be absolute URNs in the urn:message namespace.", parameterName);

        return Array.AsReadOnly(snapshot);
    }

    static bool IsMessageContractUrn(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.StartsWith("urn:message:", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && uri.Scheme.Equals("urn", StringComparison.OrdinalIgnoreCase);
    }
}
