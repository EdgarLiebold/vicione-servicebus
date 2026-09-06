using System;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>Defines the operations required by message deserializer.</summary>
public interface IMessageDeserializer :
    IProbeSite
{
    /// <summary>Gets the content type.</summary>
    ContentType ContentType { get; }

    /// <summary>Deserializes the supplied payload.</summary>
    /// <param name="receiveContext">The receive context.</param>
    /// <returns>The deserialized value.</returns>
    ConsumeContext Deserialize(ReceiveContext receiveContext);

    /// <summary>Deserializes the supplied payload.</summary>
    /// <param name="body">The body.</param>
    /// <param name="headers">The headers.</param>
    /// <param name="destinationAddress">The destination address.</param>
    /// <returns>The deserialized value.</returns>
    SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null);

    /// <summary>Returns the appropriate message body for the message deserializer, using the input type.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The message body.</returns>
    MessageBody GetMessageBody(string text);
}
