#nullable enable
namespace ViciOne.ServiceBus;

using System;


/// <summary>
/// Defines one bounded operational redrive from a receive endpoint's error queue back to the endpoint.
/// </summary>
public sealed record RabbitMqFaultRedriveRequest
{
    public const int DefaultMaxMessages = 100;
    public const int DefaultMaxScanCount = 1000;
    public const int AbsoluteMaxMessages = 1000;
    public const int AbsoluteMaxScanCount = 10000;

    public RabbitMqFaultRedriveRequest(string endpointQueueName)
    {
        EndpointQueueName = endpointQueueName ?? throw new ArgumentNullException(nameof(endpointQueueName));
    }

    /// <summary>The endpoint queue/exchange name. Its error queue is derived from bus topology.</summary>
    public string EndpointQueueName { get; init; }

    /// <summary>Maximum messages that may be republished by this operation.</summary>
    public int MaxMessages { get; init; } = DefaultMaxMessages;

    /// <summary>Maximum source messages inspected by this operation.</summary>
    public int MaxScanCount { get; init; } = DefaultMaxScanCount;

    /// <summary>Optional exact message identifier filter.</summary>
    public Guid? MessageId { get; init; }

    /// <summary>Optional exact correlation identifier filter.</summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>Optional exact fault exception type header filter.</summary>
    public string? FaultExceptionType { get; init; }
}
