using System.Collections.Frozen;
using System.Collections.Generic;

namespace ViciOne.ServiceBus;

/// <summary>
/// Configures application-level metadata and the deadline for a request.
/// </summary>
public sealed record RequestOptions
{
    /// <summary>Gets the application-defined request headers.</summary>
    public IReadOnlyDictionary<string, object?> Headers { get; init; } = FrozenDictionary<string, object?>.Empty;

    /// <summary>Gets the maximum lifetime of the request message.</summary>
    public TimeSpan? TimeToLive { get; init; }

    /// <summary>Gets the correlation identifier.</summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>Gets the conversation identifier.</summary>
    public Guid? ConversationId { get; init; }

    /// <summary>Gets the message identifier.</summary>
    public Guid? MessageId { get; init; }

    /// <summary>Gets the request identifier.</summary>
    public Guid? RequestId { get; init; }

    /// <summary>Gets the provider-neutral partition key.</summary>
    public string? PartitionKey { get; init; }

    /// <summary>Gets the absolute UTC deadline for receiving a response.</summary>
    public DateTimeOffset? Deadline { get; init; }
}
