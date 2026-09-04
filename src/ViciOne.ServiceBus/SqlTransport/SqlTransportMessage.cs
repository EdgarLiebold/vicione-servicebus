using System;
using System.Collections.Generic;
using System.Text.Json;
using ViciOne.ServiceBus.Serialization;

#nullable enable
namespace ViciOne.ServiceBus.SqlTransport;

/// <summary>
/// Provides a sql transport message implementation.
/// </summary>
public class SqlTransportMessage
{
    SendHeaders? _headers;
    SendHeaders? _transportHeaders;

    /// <summary>
    /// Gets or sets the transport message id value.
    /// </summary>
    public Guid TransportMessageId { get; set; }
    /// <summary>
    /// Gets or sets the queue name value.
    /// </summary>
    public string QueueName { get; set; } = null!;
    /// <summary>
    /// Gets or sets the priority value.
    /// </summary>
    public short Priority { get; set; }
    /// <summary>
    /// Gets or sets the message delivery id value.
    /// </summary>
    public long MessageDeliveryId { get; set; }
    /// <summary>
    /// Gets or sets the consumer id value.
    /// </summary>
    public Guid? ConsumerId { get; set; }
    /// <summary>
    /// Gets or sets the lock id value.
    /// </summary>
    public Guid? LockId { get; set; }
    /// <summary>
    /// Gets or sets the enqueue time value.
    /// </summary>
    public DateTimeOffset EnqueueTime { get; set; }
    /// <summary>
    /// Gets or sets the delivery count value.
    /// </summary>
    public int DeliveryCount { get; set; }

    /// <summary>
    /// Gets or sets the partition key value.
    /// </summary>
    public string? PartitionKey { get; set; }
    /// <summary>
    /// Gets or sets the routing key value.
    /// </summary>
    public string? RoutingKey { get; set; }

    /// <summary>
    /// Gets or sets the transport headers value.
    /// </summary>
    public string? TransportHeaders { get; set; }

    /// <summary>
    /// Gets or sets the content type value.
    /// </summary>
    public string? ContentType { get; set; }
    /// <summary>
    /// Gets or sets the message type value.
    /// </summary>
    public string? MessageType { get; set; }
    /// <summary>
    /// Gets or sets the body value.
    /// </summary>
    public string? Body { get; set; }
    /// <summary>
    /// Gets or sets the binary body value.
    /// </summary>
    public byte[]? BinaryBody { get; set; }

    /// <summary>
    /// Gets or sets the headers value.
    /// </summary>
    public string? Headers { get; set; }
    /// <summary>
    /// Gets or sets the host value.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// Gets or sets the message id value.
    /// </summary>
    public Guid? MessageId { get; set; }
    /// <summary>
    /// Gets or sets the request id value.
    /// </summary>
    public Guid? RequestId { get; set; }
    /// <summary>
    /// Gets or sets the correlation id value.
    /// </summary>
    public Guid? CorrelationId { get; set; }
    /// <summary>
    /// Gets or sets the conversation id value.
    /// </summary>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the initiator id value.
    /// </summary>
    public Guid? InitiatorId { get; set; }

    /// <summary>
    /// Gets or sets the expiration time value.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// Gets or sets the source address value.
    /// </summary>
    public Uri? SourceAddress { get; set; }
    /// <summary>
    /// Gets or sets the destination address value.
    /// </summary>
    public Uri? DestinationAddress { get; set; }
    /// <summary>
    /// Gets or sets the response address value.
    /// </summary>
    public Uri? ResponseAddress { get; set; }
    /// <summary>
    /// Gets or sets the fault address value.
    /// </summary>
    public Uri? FaultAddress { get; set; }

    /// <summary>
    /// Gets or sets the sent time value.
    /// </summary>
    public DateTimeOffset? SentTime { get; set; }

    /// <summary>
    /// Gets headers.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public SendHeaders GetHeaders()
    {
        return _headers ??= DeserializeHeaders(Headers);
    }

    /// <summary>
    /// Gets transport headers.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public SendHeaders GetTransportHeaders()
    {
        return _transportHeaders ??= DeserializeHeaders(TransportHeaders);
    }

    /// <summary>
    /// Performs the deserialize headers operation.
    /// </summary>
    /// <param name="jsonHeaders">The json headers value.</param>
    /// <returns>The result of the operation.</returns>
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

    HostInfo? GetHost()
    {
        if (Host == null)
            return null;

        return JsonSerializer.Deserialize<HostInfo>(Host, ServiceBusMetadataJson.Options);
    }

    static Uri? ToUri(string? value)
    {
        try
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : new Uri(value);
        }
        catch (FormatException)
        {
            return default;
        }
    }
}
