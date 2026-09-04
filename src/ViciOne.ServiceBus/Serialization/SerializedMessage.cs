using System;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// The content of a serialized message
/// </summary>
public interface SerializedMessage
{
    /// <summary>
    /// The destination for the serialized message
    /// </summary>
    Uri Destination { get; }

    /// <summary>
    /// The content type of the serializer used
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Gets the expiration time value.
    /// </summary>
    string ExpirationTime { get; }
    /// <summary>
    /// Gets the response address value.
    /// </summary>
    string ResponseAddress { get; }
    /// <summary>
    /// Gets the fault address value.
    /// </summary>
    string FaultAddress { get; }
    /// <summary>
    /// Gets the body value.
    /// </summary>
    string Body { get; }
    /// <summary>
    /// Gets the message id value.
    /// </summary>
    string MessageId { get; }
    /// <summary>
    /// Gets the request id value.
    /// </summary>
    string RequestId { get; }
    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    string CorrelationId { get; }
    /// <summary>
    /// Gets the conversation id value.
    /// </summary>
    string ConversationId { get; }
    /// <summary>
    /// Gets the initiator id value.
    /// </summary>
    string InitiatorId { get; }
    /// <summary>
    /// Gets the headers as json value.
    /// </summary>
    string HeadersAsJson { get; }
    /// <summary>
    /// Gets the payload message headers as json value.
    /// </summary>
    string PayloadMessageHeadersAsJson { get; }
}
