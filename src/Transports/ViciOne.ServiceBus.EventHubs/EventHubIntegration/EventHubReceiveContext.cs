using System;
using System.Collections.Generic;
using System.Net.Mime;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.EventHubs;

/// <summary>Adapts an Azure SDK event and partition context to the service-bus receive context.</summary>
public sealed class EventHubReceiveContext :
    BaseReceiveContext,
    EventHubConsumeContext
{
    readonly MessageBody _body;
    readonly ProcessEventArgs _eventArgs;
    readonly EventData _eventData;

    /// <summary>Creates a receive context for one processed Event Hubs event.</summary>
    /// <param name="eventArgs">The Azure SDK event-processing arguments.</param>
    /// <param name="receiveEndpointContext">The owning receive-endpoint context.</param>
    public EventHubReceiveContext(ProcessEventArgs eventArgs, ReceiveEndpointContext receiveEndpointContext)
        : base(false, receiveEndpointContext)
    {
        _eventArgs = eventArgs;
        _eventData = eventArgs.Data;

        _body = new BinaryMessageBody(eventArgs.Data.Body);
    }

    /// <summary>Gets a provider that exposes Event Hubs identifiers and application properties as message headers.</summary>
    protected override IHeaderProvider HeaderProvider => new EventHubHeaderProvider(_eventData);

    /// <summary>Gets the event body after applying configured inbound message limits.</summary>
    public override MessageBody Body => EnforceMessageLimits(_body);

    /// <summary>Gets the UTC instant at which Event Hubs accepted the event.</summary>
    public DateTimeOffset EnqueuedTime => _eventData.EnqueuedTime;

    /// <summary>Gets the event's provider-defined partition offset.</summary>
    public string OffsetString => _eventData.OffsetString;
    /// <summary>Gets the identifier of the partition that supplied the event.</summary>
    public string PartitionId => _eventArgs.Partition.PartitionId;
    /// <summary>Gets the partition key attached to the event.</summary>
    public string PartitionKey => _eventData.PartitionKey;
    /// <summary>Gets the event's application properties.</summary>
    public IDictionary<string, object> Properties => _eventData.Properties;
    /// <summary>Gets the event's sequence number within its partition.</summary>
    public long SequenceNumber => _eventData.SequenceNumber;
    /// <summary>Gets the event's system-managed Event Hubs properties.</summary>
    public IReadOnlyDictionary<string, object> SystemProperties => _eventData.SystemProperties;

    /// <summary>Parses the provider content type or falls back to base receive-context detection.</summary>
    /// <returns>The content type used to deserialize the event body.</returns>
    protected override ContentType GetContentType()
    {
        ContentType? contentType = default;
        if (!string.IsNullOrWhiteSpace(_eventData.ContentType))
            contentType = ConvertToContentType(_eventData.ContentType);

        return contentType ?? base.GetContentType();
    }
}
