using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Defines the contract for serializer factory.
/// </summary>
public interface ISerializerFactory
{
    /// <summary>
    /// Gets the content type value.
    /// </summary>
    ContentType ContentType { get; }

    /// <summary>
    /// Creates serializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IMessageSerializer CreateSerializer();

    /// <summary>
    /// Creates deserializer.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    IMessageDeserializer CreateDeserializer();
}
