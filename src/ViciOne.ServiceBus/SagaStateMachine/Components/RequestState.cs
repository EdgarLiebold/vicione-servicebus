using System;

namespace ViciOne.ServiceBus.Components;

/// <summary>Carries state for request.</summary>
public class RequestState :
    SagaStateMachineInstance,
    ISagaVersion
{
    /// <summary>Gets or sets the current state.</summary>
    public int CurrentState { get; set; }

    /// <summary>Gets or sets the conversation id.</summary>
    public Guid? ConversationId { get; set; }
    /// <summary>Gets or sets the response address.</summary>
    public Uri ResponseAddress { get; set; } = null!;
    /// <summary>Gets or sets the fault address.</summary>
    public Uri FaultAddress { get; set; } = null!;
    /// <summary>Gets or sets the expiration time.</summary>
    public DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>The correlationId of the original saga instance.</summary>
    public Guid SagaCorrelationId { get; set; }

    /// <summary>The saga address where the request should be redelivered.</summary>
    public Uri SagaAddress { get; set; } = null!;
    /// <summary>Gets or sets the version.</summary>
    public int Version { get; set; }

    /// <summary>Same as RequestId from the original request.</summary>
    public Guid CorrelationId { get; set; }
}
