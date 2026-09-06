using System.Collections.Frozen;
using System.Collections.Generic;

namespace ViciOne.ServiceBus;

/// <summary>Configures application-level metadata and the deadline for a request.</summary>
public sealed record RequestOptions
{
    /// <summary>Gets or sets the headers.</summary>
    public IReadOnlyDictionary<string, object?> Headers { get; init; } = FrozenDictionary<string, object?>.Empty;

    /// <summary>Gets or sets the time to live.</summary>
    public TimeSpan? TimeToLive { get; init; }

    /// <summary>Gets or sets the correlation id.</summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>Gets or sets the conversation id.</summary>
    public Guid? ConversationId { get; init; }

    /// <summary>Gets or sets the message id.</summary>
    public Guid? MessageId { get; init; }

    /// <summary>Gets or sets the request id.</summary>
    public Guid? RequestId { get; init; }

    /// <summary>Gets or sets the partition key.</summary>
    public string? PartitionKey { get; init; }

    /// <summary>Gets or sets the deadline.</summary>
    public DateTimeOffset? Deadline { get; init; }
}
