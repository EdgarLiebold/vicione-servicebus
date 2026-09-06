using System.Collections.Frozen;
using System.Collections.Generic;

namespace ViciOne.ServiceBus;

/// <summary>Configures application-level metadata for a published message.</summary>
public sealed record PublishOptions
{
    /// <summary>Gets the application headers to add to the published message.</summary>
    public IReadOnlyDictionary<string, object?> Headers { get; init; } = FrozenDictionary<string, object?>.Empty;

    /// <summary>Gets the duration for which the published message remains eligible for delivery.</summary>
    public TimeSpan? TimeToLive { get; init; }

    /// <summary>Gets the identifier that correlates the message with related work.</summary>
    public Guid? CorrelationId { get; init; }

    /// <summary>Gets the identifier shared by messages in the same conversation.</summary>
    public Guid? ConversationId { get; init; }

    /// <summary>Gets the unique identifier assigned to the published message.</summary>
    public Guid? MessageId { get; init; }

    /// <summary>Gets the request identifier propagated by the publication.</summary>
    public Guid? RequestId { get; init; }

    /// <summary>Gets the transport partition key, when the selected transport supports partitioning.</summary>
    public string? PartitionKey { get; init; }
}
