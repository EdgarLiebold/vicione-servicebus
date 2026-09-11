using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Represents an Amazon SQS send context for a typed message.</summary>
/// <typeparam name="T">The message type.</typeparam>
public class AmazonSqsMessageSendContext<T> :
    MessageSendContext<T>,
    AmazonSqsSendContext<T>
    where T : class
{
    /// <summary>Initializes an Amazon SQS send context for a message.</summary>
    /// <param name="message">The message being sent.</param>
    /// <param name="cancellationToken">The token used to cancel the send operation.</param>
    public AmazonSqsMessageSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
    }

    /// <summary>Gets or sets the FIFO message-group identifier.</summary>
    public string? GroupId { get; set; }
    /// <summary>Gets or sets the FIFO message-deduplication identifier.</summary>
    public string? DeduplicationId { get; set; }

    /// <summary>Sets the delivery delay in seconds; setting <see langword="null"/> clears the delay.</summary>
    public int? DelaySeconds
    {
        set => Delay = value.HasValue
            ? TimeSpan.FromSeconds(AmazonSqsDelay.FromTimeSpan(TimeSpan.FromSeconds(value.Value)))
            : null;
    }

    /// <summary>Restores the Amazon SQS FIFO identifiers from transport properties.</summary>
    /// <param name="properties">The transport properties to read.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        GroupId = ReadString(properties, AmazonSqsTransportPropertyNames.GroupId);
        DeduplicationId = ReadString(properties, AmazonSqsTransportPropertyNames.DeduplicationId);
    }

    /// <summary>Writes nonempty Amazon SQS FIFO identifiers to transport properties.</summary>
    /// <param name="properties">The transport properties to update.</param>
    public override void WritePropertiesTo(IDictionary<string, object> properties)
    {
        base.WritePropertiesTo(properties);

        if (!string.IsNullOrWhiteSpace(GroupId))
            properties[AmazonSqsTransportPropertyNames.GroupId] = GroupId!;
        if (!string.IsNullOrWhiteSpace(DeduplicationId))
            properties[AmazonSqsTransportPropertyNames.DeduplicationId] = DeduplicationId!;
    }
}
