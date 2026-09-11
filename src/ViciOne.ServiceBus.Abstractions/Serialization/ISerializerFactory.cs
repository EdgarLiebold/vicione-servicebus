using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Creates paired message serializers and deserializers for one media content type.</summary>
public interface ISerializerFactory
{
    /// <summary>Gets the media content type supported by the created instances.</summary>
    ContentType ContentType { get; }

    /// <summary>Creates a message serializer.</summary>
    /// <returns>A serializer for <see cref="ContentType" />.</returns>
    IMessageSerializer CreateSerializer();

    /// <summary>Creates a message deserializer.</summary>
    /// <returns>A deserializer for <see cref="ContentType" />.</returns>
    IMessageDeserializer CreateDeserializer();
}
