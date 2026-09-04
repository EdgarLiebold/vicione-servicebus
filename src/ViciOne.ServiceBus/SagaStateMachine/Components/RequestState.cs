using System;

namespace ViciOne.ServiceBus.Components;

/// <summary>
/// Provides a request state implementation.
/// </summary>
public class RequestState :
    SagaStateMachineInstance,
    ISagaVersion
{
    /// <summary>
    /// Gets or sets the current state value.
    /// </summary>
    public int CurrentState { get; set; }

    /// <summary>
    /// Gets or sets the conversation id value.
    /// </summary>
    public Guid? ConversationId { get; set; }
    /// <summary>
    /// Gets or sets the response address value.
    /// </summary>
    public Uri ResponseAddress { get; set; } = null!;
    /// <summary>
    /// Gets or sets the fault address value.
    /// </summary>
    public Uri FaultAddress { get; set; } = null!;
    /// <summary>
    /// Gets or sets the expiration time value.
    /// </summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// The correlationId of the original saga instance
    /// </summary>
    public Guid SagaCorrelationId { get; set; }

    /// <summary>
    /// The saga address where the request should be redelivered
    /// </summary>
    public Uri SagaAddress { get; set; } = null!;
    /// <summary>
    /// Gets or sets the version value.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Same as RequestId from the original request
    /// </summary>
    public Guid CorrelationId { get; set; }
}
