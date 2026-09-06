using System.Collections.Frozen;
using System.Collections.Generic;

namespace ViciOne.ServiceBus;

/// <summary>Configures application-level metadata and the deadline for a request.</summary>
public sealed record RequestOptions
{
    /// <summary>Gets the application headers to add to the request.</summary>
    public IReadOnlyDictionary<string, object?> Headers { get; init; } = FrozenDictionary<string, object?>.Empty;

    /// <summary>Gets the duration for which the request remains eligible for delivery.</summary>
    public TimeSpan? TimeToLive { get; init; }

    /// <summary>Gets the identifier that correlates the request with related work.</summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>Gets the identifier shared by messages in the same conversation.</summary>
    public Guid? ConversationId { get; init; }

    /// <summary>Gets the unique identifier assigned to the request message.</summary>
    public Guid? MessageId { get; init; }

    /// <summary>Gets the identifier used to match responses with this request.</summary>
    public Guid? RequestId { get; init; }

    /// <summary>Gets the transport partition key, when the destination supports partitioning.</summary>
    public string? PartitionKey { get; init; }

    /// <summary>Gets the absolute deadline after which the request is no longer valid.</summary>
    public DateTimeOffset? Deadline { get; init; }
}
