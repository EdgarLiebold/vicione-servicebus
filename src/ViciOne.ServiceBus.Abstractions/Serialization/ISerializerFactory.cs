using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Creates serializer instances.</summary>
public interface ISerializerFactory
{
    /// <summary>Gets the content type.</summary>
    ContentType ContentType { get; }

    /// <summary>Creates serializer.</summary>
    /// <returns>The created serializer.</returns>
    IMessageSerializer CreateSerializer();

    /// <summary>Creates deserializer.</summary>
    /// <returns>The created deserializer.</returns>
    IMessageDeserializer CreateDeserializer();
}
