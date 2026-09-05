using System;
using System.Collections.Generic;
using System.Net.Mime;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>
/// Provides an event hub receive context implementation.
/// </summary>
public sealed class EventHubReceiveContext :
    BaseReceiveContext,
    EventHubConsumeContext
{
    readonly MessageBody _body;
    readonly ProcessEventArgs _eventArgs;
    readonly EventData _eventData;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="eventArgs">The event args value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    public EventHubReceiveContext(ProcessEventArgs eventArgs, ReceiveEndpointContext receiveEndpointContext)
        : base(false, receiveEndpointContext)
    {
        _eventArgs = eventArgs;
        _eventData = eventArgs.Data;

        _body = new MemoryMessageBody(eventArgs.Data.Body);
    }

    /// <summary>
    /// Gets the header provider value.
    /// </summary>
    protected override IHeaderProvider HeaderProvider => new EventHubHeaderProvider(_eventData);

    /// <summary>
    /// Gets the body value.
    /// </summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    /// <summary>
    /// Gets the enqueued time value.
    /// </summary>
    public DateTimeOffset EnqueuedTime => _eventData.EnqueuedTime;

    /// <summary>
    /// Gets the offset string value.
    /// </summary>
    public string OffsetString => _eventData.OffsetString;
    /// <summary>
    /// Gets the partition id value.
    /// </summary>
    public string PartitionId => _eventArgs.Partition.PartitionId;
    /// <summary>
    /// Gets the partition key value.
    /// </summary>
    public string PartitionKey => _eventData.PartitionKey;
    /// <summary>
    /// Gets the properties value.
    /// </summary>
    public IDictionary<string, object> Properties => _eventData.Properties;
    /// <summary>
    /// Gets the sequence number value.
    /// </summary>
    public long SequenceNumber => _eventData.SequenceNumber;
    /// <summary>
    /// Gets the system properties value.
    /// </summary>
    public IReadOnlyDictionary<string, object> SystemProperties => _eventData.SystemProperties;

    /// <summary>
    /// Gets content type.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override ContentType GetContentType()
    {
        ContentType? contentType = default;
        if (!string.IsNullOrWhiteSpace(_eventData.ContentType))
            contentType = ConvertToContentType(_eventData.ContentType);

        return contentType ?? base.GetContentType();
    }
}
