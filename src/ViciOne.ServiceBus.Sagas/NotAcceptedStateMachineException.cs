using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports that a saga state machine rejected a message in its current state.</summary>
public sealed class NotAcceptedStateMachineException :
    SagaException
{
    /// <summary>Creates an exception for a message rejected by a saga state.</summary>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="correlationId">The identifier of the saga instance.</param>
    /// <param name="currentState">The state that rejected the message.</param>
    /// <param name="exception">The exception raised while evaluating the message.</param>
    public NotAcceptedStateMachineException(Type sagaType, Type messageType, Guid correlationId, string currentState, Exception exception)
        : base(FormatMessage(currentState), sagaType, messageType, correlationId, exception)
    {
        CurrentState = currentState;
    }

    /// <summary>Gets the state that rejected the message.</summary>
    public string CurrentState { get; }

    static string FormatMessage(string currentState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentState);

        return $"Not accepted in state {currentState}";
    }
}
