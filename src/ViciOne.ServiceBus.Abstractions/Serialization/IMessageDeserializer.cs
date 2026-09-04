using System;
using System.Net.Mime;

namespace ViciOne.ServiceBus.Advanced.Serialization;

/// <summary>
/// Defines the contract for message deserializer.
/// </summary>
public interface IMessageDeserializer :
    IProbeSite
{
    /// <summary>
    /// Gets the content type value.
    /// </summary>
    ContentType ContentType { get; }

    /// <summary>
    /// Performs the deserialize operation.
    /// </summary>
    /// <param name="receiveContext">The receive context value.</param>
    /// <returns>The result of the operation.</returns>
    ConsumeContext Deserialize(ReceiveContext receiveContext);

    /// <summary>
    /// Performs the deserialize operation.
    /// </summary>
    /// <param name="body">The body value.</param>
    /// <param name="headers">The headers value.</param>
    /// <param name="destinationAddress">The destination address value.</param>
    /// <returns>The result of the operation.</returns>
    SerializerContext Deserialize(MessageBody body, Headers headers, Uri? destinationAddress = null);

    /// <summary>
    /// Returns the appropriate message body for the message deserializer, using the input type
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    MessageBody GetMessageBody(string text);
}
