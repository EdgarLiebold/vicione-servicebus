// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class NotAcceptedStateMachineException :
        SagaException
    {
        public NotAcceptedStateMachineException(Type sagaType, Type messageType, Guid correlationId, string currentState, Exception exception)
            : base($"Not accepted in state {currentState}", sagaType, messageType, correlationId, exception)
        {
        }
    }
}
