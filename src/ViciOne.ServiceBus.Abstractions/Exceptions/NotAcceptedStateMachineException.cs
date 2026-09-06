using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to not accepted state machine.
/// </summary>
public class NotAcceptedStateMachineException :
    SagaException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="sagaType">The saga type value.</param>
    /// <param name="messageType">The message type value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="currentState">The current state value.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public NotAcceptedStateMachineException(Type sagaType, Type messageType, Guid correlationId, string currentState, Exception exception)
        : base($"Not accepted in state {currentState}", sagaType, messageType, correlationId, exception)
    {
    }
}
