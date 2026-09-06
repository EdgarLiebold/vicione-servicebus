using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to not accepted state machine.</summary>
public class NotAcceptedStateMachineException :
    SagaException
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="currentState">The current state.</param>
    /// <param name="exception">The exception associated with the operation.</param>
    public NotAcceptedStateMachineException(Type sagaType, Type messageType, Guid correlationId, string currentState, Exception exception)
        : base($"Not accepted in state {currentState}", sagaType, messageType, correlationId, exception)
    {
    }
}
