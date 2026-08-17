namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class UnknownStateException :
        SagaStateMachineException
    {
        public UnknownStateException()
        {
        }

        public UnknownStateException(string machineType, string stateName)
            : base($"The {stateName} state is not defined for the {machineType} state machine")
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected UnknownStateException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
