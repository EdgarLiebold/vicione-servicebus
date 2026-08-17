namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class SagaStateMachineException :
        ViciOneServiceBusException
    {
        public SagaStateMachineException()
        {
        }

        public SagaStateMachineException(string message)
            : base(message)
        {
        }

        public SagaStateMachineException(Type machineType, string message)
            : base($"{machineType.Name}: {message}")
        {
        }

        public SagaStateMachineException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public SagaStateMachineException(Type machineType, string message, Exception innerException)
            : base($"{machineType.Name}: {message}", innerException)
        {
        }
    }
}
