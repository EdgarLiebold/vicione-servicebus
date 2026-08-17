namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class EventExecutionException :
        SagaStateMachineException
    {
        public EventExecutionException(string message)
            : base(message)
        {
        }

        public EventExecutionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public EventExecutionException()
        {
        }
    }
}
