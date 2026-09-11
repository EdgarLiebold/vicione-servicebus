using System;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Converts transport bodies into consume and serializer contexts for one media content type.</summary>
public interface IMessageDeserializer :
    IProbeSite
{
    /// <summary>Gets the media content type accepted by this deserializer.</summary>
    ContentType ContentType { get; }

    /// <summary>Deserializes a received transport envelope into a consume context.</summary>
    /// <param name="receiveContext">The receive context containing the transport body and metadata.</param>
    /// <returns>The consume context created from the envelope.</returns>
    ConsumeContext Deserialize(ReceiveContext receiveContext);

    /// <summary>Deserializes a message body and transport headers into a serializer context.</summary>
    /// <param name="body">The serialized message body.</param>
    /// <param name="headers">The transport headers associated with the body.</param>
    /// <param name="destinationAddress">The destination address associated with the serialized message.</param>
    /// <returns>The serializer context created from the supplied body and metadata.</returns>
    SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null);

    /// <summary>Creates this deserializer's message-body representation from serialized text.</summary>
    /// <param name="text">The serialized message text.</param>
    /// <returns>A message body containing the supplied serialized representation.</returns>
    MessageBody GetMessageBody(string text);
}
