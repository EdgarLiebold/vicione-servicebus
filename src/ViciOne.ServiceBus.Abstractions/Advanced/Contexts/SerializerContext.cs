using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides deserialized message content and serializer-specific forwarding operations.</summary>
public interface SerializerContext :
    MessageContext,
    IObjectDeserializer
{
    /// <summary>Gets the message-type identifiers declared by the serialized envelope.</summary>
    string[] SupportedMessageTypes { get; }

    /// <summary>Determines whether the envelope supports a message contract.</summary>
    /// <typeparam name="TMessage">The message contract type.</typeparam>
    /// <returns><see langword="true" /> when the message contract is supported; otherwise, <see langword="false" />.</returns>
    bool IsSupportedMessageType<TMessage>()
        where TMessage : class;

    /// <summary>Determines whether the envelope supports a runtime message contract.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns><see langword="true" /> when the message contract is supported; otherwise, <see langword="false" />.</returns>
    bool IsSupportedMessageType(Type messageType);

    /// <summary>Tries to deserialize the message as a contract type.</summary>
    /// <typeparam name="TMessage">The requested message contract type.</typeparam>
    /// <param name="message">The deserialized message when successful.</param>
    /// <returns><see langword="true" /> when the message was deserialized; otherwise, <see langword="false" />.</returns>
    bool TryGetMessage<TMessage>([NotNullWhen(true)] out TMessage? message)
        where TMessage : class;

    /// <summary>Tries to deserialize the message as a runtime contract type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="message">The deserialized message when successful.</param>
    /// <returns><see langword="true" /> when the message was deserialized; otherwise, <see langword="false" />.</returns>
    bool TryGetMessage(Type messageType, [NotNullWhen(true)] out object? message);

    /// <summary>
    /// Gets a serializer that preserves the received content type when forwarding the message.
    /// </summary>
    /// <returns>The forwarding serializer.</returns>
    IMessageSerializer GetMessageSerializer();

    /// <summary>
    /// Gets a serializer that overlays a typed message on an envelope while preserving the received content type.
    /// </summary>
    /// <typeparam name="TMessage">The overlay message type.</typeparam>
    /// <param name="envelope">The source envelope.</param>
    /// <param name="message">The message whose properties overlay the envelope content.</param>
    /// <returns>The forwarding serializer.</returns>
    IMessageSerializer GetMessageSerializer<TMessage>(MessageEnvelope envelope, TMessage message)
        where TMessage : class;

    /// <summary>
    /// Gets a serializer for forwarding an untyped message with explicit message-type identifiers.
    /// </summary>
    /// <param name="message">The message to forward.</param>
    /// <param name="messageTypes">The message-type identifiers declared for the forwarded message.</param>
    /// <returns>The forwarding serializer.</returns>
    IMessageSerializer GetMessageSerializer(object message, string[] messageTypes);

    /// <summary>
    /// Converts a message to the serializer's property representation.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="message">The message to convert.</param>
    /// <returns>A property-name/value representation understood by the serializer.</returns>
    Dictionary<string, object> ToDictionary<TMessage>(TMessage? message)
        where TMessage : class;
}
