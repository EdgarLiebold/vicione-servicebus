using System;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>The content of a serialized message.</summary>
public interface SerializedMessage
{
    /// <summary>The destination for the serialized message.</summary>
    Uri Destination { get; }

    /// <summary>The content type of the serializer used.</summary>
    string ContentType { get; }

    /// <summary>Gets the expiration time.</summary>
    string ExpirationTime { get; }
    /// <summary>Gets the response address.</summary>
    string ResponseAddress { get; }
    /// <summary>Gets the fault address.</summary>
    string FaultAddress { get; }
    /// <summary>Gets the body.</summary>
    string Body { get; }
    /// <summary>Gets the message id.</summary>
    string MessageId { get; }
    /// <summary>Gets the request id.</summary>
    string RequestId { get; }
    /// <summary>Gets the correlation id.</summary>
    string CorrelationId { get; }
    /// <summary>Gets the conversation id.</summary>
    string ConversationId { get; }
    /// <summary>Gets the initiator id.</summary>
    string InitiatorId { get; }
    /// <summary>Gets the headers as json.</summary>
    string HeadersAsJson { get; }
    /// <summary>Gets the payload message headers as json.</summary>
    string PayloadMessageHeadersAsJson { get; }
}
