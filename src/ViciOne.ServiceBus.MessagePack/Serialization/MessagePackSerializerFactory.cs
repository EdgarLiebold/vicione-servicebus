using System;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Provides a message pack serializer factory implementation.
/// </summary>
public class MessagePackSerializerFactory
    : ISerializerFactory
{
    /// <summary>
    /// Gets the content type value.
    /// </summary>
    public ContentType ContentType => MessagePackMessageSerializer.MessagePackContentType;

    readonly Lazy<MessagePackMessageSerializer> _serializer;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessagePackSerializerFactory()
    {
        _serializer = new Lazy<MessagePackMessageSerializer>(() => new MessagePackMessageSerializer());
    }

    /// <summary>
    /// Creates serializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IMessageSerializer CreateSerializer()
    {
        return _serializer.Value;
    }

    /// <summary>
    /// Creates deserializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IMessageDeserializer CreateDeserializer()
    {
        return _serializer.Value;
    }
}
