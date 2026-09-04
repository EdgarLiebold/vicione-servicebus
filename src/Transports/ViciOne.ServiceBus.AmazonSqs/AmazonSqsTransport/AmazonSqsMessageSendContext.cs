using System;
using System.Collections.Generic;
using System.Threading;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Provides an amazon sqs message send context implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class AmazonSqsMessageSendContext<T> :
    MessageSendContext<T>,
    AmazonSqsSendContext<T>
    where T : class
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public AmazonSqsMessageSendContext(T message, CancellationToken cancellationToken)
        : base(message, cancellationToken)
    {
    }

    /// <summary>
    /// Gets or sets the group id value.
    /// </summary>
    public string? GroupId { get; set; }
    /// <summary>
    /// Gets or sets the deduplication id value.
    /// </summary>
    public string? DeduplicationId { get; set; }

    /// <summary>
    /// Gets or sets the delay seconds value.
    /// </summary>
    public int? DelaySeconds
    {
        set => Delay = value.HasValue
            ? TimeSpan.FromSeconds(AmazonSqsDelay.FromTimeSpan(TimeSpan.FromSeconds(value.Value)))
            : null;
    }

    /// <summary>
    /// Performs the read properties from operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    public override void ReadPropertiesFrom(IReadOnlyDictionary<string, object> properties)
    {
        base.ReadPropertiesFrom(properties);

        GroupId = ReadString(properties, AmazonSqsTransportPropertyNames.GroupId);
        DeduplicationId = ReadString(properties, AmazonSqsTransportPropertyNames.DeduplicationId);
    }

    /// <summary>
    /// Performs the write properties to operation.
    /// </summary>
    /// <param name="properties">The properties value.</param>
    public override void WritePropertiesTo(IDictionary<string, object> properties)
    {
        base.WritePropertiesTo(properties);

        if (!string.IsNullOrWhiteSpace(GroupId))
            properties[AmazonSqsTransportPropertyNames.GroupId] = GroupId!;
        if (!string.IsNullOrWhiteSpace(DeduplicationId))
            properties[AmazonSqsTransportPropertyNames.DeduplicationId] = DeduplicationId!;
    }
}
