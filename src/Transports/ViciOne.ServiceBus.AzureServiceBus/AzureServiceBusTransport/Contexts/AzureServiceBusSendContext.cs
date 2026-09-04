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


/// <summary>
/// Provides an azure service bus send context implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class AzureServiceBusSendContext<T> :
    MessageSendContext<T>,
    ServiceBusSendContext<T>
    where T : class
{
    string? _partitionKey;
    string? _sessionId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public AzureServiceBusSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
    }

    /// <summary>
    /// Gets or sets the delay value.
    /// </summary>
    public override TimeSpan? Delay
    {
        get => ScheduledEnqueueTimeUtc.HasValue ? ScheduledEnqueueTimeUtc.Value - this.GetTimeProvider().GetUtcNow() : default;
        set => ScheduledEnqueueTimeUtc = value is { } delay && delay > TimeSpan.Zero
            ? this.GetTimeProvider().GetUtcNow() + delay
            : default(DateTimeOffset?);
    }

    /// <summary>
    /// Gets or sets the reply to session id value.
    /// </summary>
    public string? ReplyToSessionId { get; set; }
    /// <summary>
    /// Gets or sets the reply to value.
    /// </summary>
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Gets or sets the scheduled enqueue time utc value.
    /// </summary>
    public DateTimeOffset? ScheduledEnqueueTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the label value.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Gets or sets the partition key value.
    /// </summary>
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

    /// <summary>
    /// Gets or sets the session id value.
    /// </summary>
    public string? SessionId
    {
        get => _sessionId;
        set
        {
            _partitionKey = value;
            _sessionId = value;
        }
    }

    /// <summary>
    /// Sets scheduled message id.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number value.</param>
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

    /// <summary>
    /// Attempts to get scheduled message id.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetScheduledMessageId(out long sequenceNumber)
    {
        if (ScheduledMessageId.HasValue)
            return TryGetSequenceNumber(ScheduledMessageId.Value, out sequenceNumber);

        sequenceNumber = 0;
        return false;
    }

    /// <summary>
    /// Attempts to get sequence number.
    /// </summary>
    /// <param name="id">The id value.</param>
    /// <param name="sequenceNumber">The sequence number value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Performs the read properties from operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
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

    /// <summary>
    /// Performs the write properties to operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
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
