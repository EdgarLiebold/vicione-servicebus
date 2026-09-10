using System;
using System.Collections.Generic;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>Carries sql transport message data.</summary>
public class SqlTransportMessage
{
    SendHeaders? _headers;
    SendHeaders? _transportHeaders;

    /// <summary>Gets or sets the transport message id.</summary>
    public Guid TransportMessageId { get; set; }
    /// <summary>Gets or sets the queue name.</summary>
    public string QueueName { get; set; } = null!;
    /// <summary>Gets or sets the priority.</summary>
    public short Priority { get; set; }
    /// <summary>Gets or sets the message delivery id.</summary>
    public long MessageDeliveryId { get; set; }
    /// <summary>Gets or sets the consumer id.</summary>
    public Guid? ConsumerId { get; set; }
    /// <summary>Gets or sets the lock id.</summary>
    public Guid? LockId { get; set; }
    /// <summary>Gets or sets the enqueue time.</summary>
    public DateTimeOffset EnqueueTime { get; set; }
    /// <summary>Gets or sets the delivery count.</summary>
    public int DeliveryCount { get; set; }

    /// <summary>Gets or sets the partition key.</summary>
    public string? PartitionKey { get; set; }
    /// <summary>Gets or sets the routing key.</summary>
    public string? RoutingKey { get; set; }

    /// <summary>Gets or sets the transport headers.</summary>
    public string? TransportHeaders { get; set; }

    /// <summary>Gets or sets the content type.</summary>
    public string? ContentType { get; set; }
    /// <summary>Gets or sets the message type.</summary>
    public string? MessageType { get; set; }
    /// <summary>Gets or sets the body.</summary>
    public string? Body { get; set; }
    /// <summary>Gets or sets the binary body.</summary>
    public byte[]? BinaryBody { get; set; }

    /// <summary>Gets or sets the headers.</summary>
    public string? Headers { get; set; }
    /// <summary>Gets or sets the host.</summary>
    public string? Host { get; set; }

    /// <summary>Gets or sets the message id.</summary>
    public Guid? MessageId { get; set; }
    /// <summary>Gets or sets the request id.</summary>
    public Guid? RequestId { get; set; }
    /// <summary>Gets or sets the correlation id.</summary>
    public Guid? CorrelationId { get; set; }
    /// <summary>Gets or sets the conversation id.</summary>
    public Guid? ConversationId { get; set; }
    /// <summary>Gets or sets the initiator id.</summary>
    public Guid? InitiatorId { get; set; }

    /// <summary>Gets or sets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>Gets or sets the source address.</summary>
    public Uri? SourceAddress { get; set; }
    /// <summary>Gets or sets the destination address.</summary>
    public Uri? DestinationAddress { get; set; }
    /// <summary>Gets or sets the response address.</summary>
    public Uri? ResponseAddress { get; set; }
    /// <summary>Gets or sets the fault address.</summary>
    public Uri? FaultAddress { get; set; }

    /// <summary>Gets or sets the sent time.</summary>
    public DateTimeOffset? SentTime { get; set; }

    /// <summary>Gets headers.</summary>
    /// <returns>The headers.</returns>
    public SendHeaders GetHeaders()
    {
        return _headers ??= DeserializeHeaders(Headers);
    }

    /// <summary>Gets transport headers.</summary>
    /// <returns>The transport headers.</returns>
    public SendHeaders GetTransportHeaders()
    {
        return _transportHeaders ??= DeserializeHeaders(TransportHeaders);
    }

    /// <summary>Deserializes headers.</summary>
    /// <param name="jsonHeaders">The json headers.</param>
    /// <returns>The deserialized headers.</returns>
    public static SendHeaders DeserializeHeaders(string? jsonHeaders)
    {
        var headers = new DictionarySendHeaders();

        if (jsonHeaders != null)
        {
            var elements = JsonSerializer.Deserialize<IEnumerable<KeyValuePair<string, object>>>(jsonHeaders, ServiceBusMetadataJson.Options);
            if (elements != null)
            {
                foreach (KeyValuePair<string, object> element in elements)
                    headers.Set(element.Key, element.Value);
            }
        }

        return headers;
    }

}
