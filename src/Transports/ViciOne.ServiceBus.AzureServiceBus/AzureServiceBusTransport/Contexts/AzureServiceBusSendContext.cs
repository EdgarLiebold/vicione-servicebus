using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.AzureServiceBus;

static class ScheduledMessageToken
{
    internal static readonly ulong Tag;
    internal static readonly byte[] Key;

    static ScheduledMessageToken()
    {
        var guid = new Guid("E25FC12B-FF28-4476-A6E1-DE45E154A675");

        Key = guid.ToByteArray();

        Tag = BitConverter.ToUInt64(Key, 8);
    }
}


/// <summary>Adds Azure Service Bus scheduling, partition, session, and reply metadata to a message send context.</summary>
/// <typeparam name="T">The message type being sent.</typeparam>
public class AzureServiceBusSendContext<T> :
    MessageSendContext<T>,
    ServiceBusSendContext<T>
    where T : class
{
    string? _partitionKey;
    string? _sessionId;

    /// <summary>Initializes a transport send context for a message.</summary>
    /// <param name="message">The message instance to send.</param>
    /// <param name="cancellationToken">The token that cancels the send.</param>
    public AzureServiceBusSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
    }

    /// <summary>Gets or sets the relative delay by translating it to an absolute scheduled enqueue time.</summary>
    public override TimeSpan? Delay
    {
        get => ScheduledEnqueueTimeUtc.HasValue ? ScheduledEnqueueTimeUtc.Value - this.GetTimeProvider().GetUtcNow() : default;
        set => ScheduledEnqueueTimeUtc = value is { } delay && delay > TimeSpan.Zero
            ? this.GetTimeProvider().GetUtcNow() + delay
            : default(DateTimeOffset?);
    }

    /// <summary>Gets or sets the session identifier expected on replies.</summary>
    public string? ReplyToSessionId { get; set; }
    /// <summary>Gets or sets the reply destination entity path.</summary>
    public string? ReplyTo { get; set; }

    /// <summary>Gets or sets the UTC time at which Azure Service Bus should enqueue the message.</summary>
    public DateTimeOffset? ScheduledEnqueueTimeUtc { get; set; }

    /// <summary>Gets or sets the transport label mapped to the message subject.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the partition key, clearing a conflicting session identifier.</summary>
    public string? PartitionKey
    {
        get => _partitionKey;
        set
        {
            _partitionKey = value;

            if (string.IsNullOrWhiteSpace(_sessionId) || string.Equals(_sessionId, value, StringComparison.Ordinal))
                return;

            _sessionId = null;
        }
    }

    /// <summary>Gets or sets the session identifier and keeps the partition key equal to it.</summary>
    public string? SessionId
    {
        get => _sessionId;
        set
        {
            _partitionKey = value;
            _sessionId = value;
        }
    }

    /// <summary>Encodes an Azure Service Bus scheduled-message sequence number into the portable scheduled-message identifier.</summary>
    /// <param name="sequenceNumber">The broker sequence number returned by scheduling.</param>
    public void SetScheduledMessageId(long sequenceNumber)
    {
        var key = ScheduledMessageToken.Key;

        var bytes = new byte[16];
        Buffer.BlockCopy(key, 8, bytes, 8, 8);

        var sequenceNumberBytes = BitConverter.GetBytes(sequenceNumber);

        var sequenceLength = sequenceNumberBytes.Length;

        Buffer.BlockCopy(sequenceNumberBytes, 0, bytes, 0, sequenceLength);

        ScheduledMessageId = new Guid(bytes);
    }

    /// <summary>Decodes the broker sequence number from this context's scheduled-message identifier.</summary>
    /// <param name="sequenceNumber">The decoded sequence number when the identifier belongs to this transport.</param>
    /// <returns><see langword="true"/> when a valid Azure Service Bus scheduling identifier is present.</returns>
    public bool TryGetScheduledMessageId(out long sequenceNumber)
    {
        if (ScheduledMessageId.HasValue)
            return TryGetSequenceNumber(ScheduledMessageId.Value, out sequenceNumber);

        sequenceNumber = 0;
        return false;
    }

    /// <summary>Decodes a sequence number from a transport-tagged scheduled-message identifier.</summary>
    /// <param name="id">The identifier to inspect.</param>
    /// <param name="sequenceNumber">The decoded sequence number when the transport tag matches.</param>
    /// <returns><see langword="true"/> when the identifier carries the Azure Service Bus transport tag.</returns>
    public bool TryGetSequenceNumber(Guid id, out long sequenceNumber)
    {
        var bytes = id.ToByteArray();

        if (BitConverter.ToUInt64(bytes, 8) == ScheduledMessageToken.Tag)
        {
            sequenceNumber = BitConverter.ToInt64(bytes, 0);
            return true;
        }

        sequenceNumber = default;
        return false;
    }

    /// <summary>Restores Azure Service Bus send metadata from persisted transport properties.</summary>
    /// <param name="properties">The persisted property bag.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        var partitionKey = ReadString(properties, AzureServiceBusTransportPropertyNames.PartitionKey);
        if (!string.IsNullOrWhiteSpace(partitionKey))
            PartitionKey = partitionKey;

        var sessionId = ReadString(properties, AzureServiceBusTransportPropertyNames.SessionId);
        if (!string.IsNullOrWhiteSpace(sessionId))
            SessionId = sessionId;

        var replyToSessionId = ReadString(properties, AzureServiceBusTransportPropertyNames.ReplyToSessionId);
        if (!string.IsNullOrWhiteSpace(replyToSessionId))
            ReplyToSessionId = replyToSessionId;

        var replyTo = ReadString(properties, AzureServiceBusTransportPropertyNames.ReplyTo);
        if (!string.IsNullOrWhiteSpace(replyTo))
            ReplyTo = replyTo;

        var label = ReadString(properties, AzureServiceBusTransportPropertyNames.Label);
        if (!string.IsNullOrWhiteSpace(label))
            Label = label;
    }

    /// <summary>Writes non-empty Azure Service Bus send metadata to a transport property bag.</summary>
    /// <param name="properties">The property bag to update.</param>
    public override void WritePropertiesTo(IDictionary<string, object> properties)
    {
        base.WritePropertiesTo(properties);

        if (!string.IsNullOrWhiteSpace(PartitionKey))
            properties[AzureServiceBusTransportPropertyNames.PartitionKey] = PartitionKey;
        if (!string.IsNullOrWhiteSpace(SessionId))
            properties[AzureServiceBusTransportPropertyNames.SessionId] = SessionId;
        if (!string.IsNullOrWhiteSpace(ReplyToSessionId))
            properties[AzureServiceBusTransportPropertyNames.ReplyToSessionId] = ReplyToSessionId;
        if (!string.IsNullOrWhiteSpace(ReplyTo))
            properties[AzureServiceBusTransportPropertyNames.ReplyTo] = ReplyTo;
        if (!string.IsNullOrWhiteSpace(Label))
            properties[AzureServiceBusTransportPropertyNames.Label] = Label;
    }
}
