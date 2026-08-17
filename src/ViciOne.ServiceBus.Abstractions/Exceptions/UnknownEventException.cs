namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class UnknownEventException :
        SagaStateMachineException
    {
        public UnknownEventException()
        {
        }

        public UnknownEventException(string machineType, string eventName)
            : base($"The {eventName} event is not defined for the {machineType} state machine")
        {
        }
    }
}
