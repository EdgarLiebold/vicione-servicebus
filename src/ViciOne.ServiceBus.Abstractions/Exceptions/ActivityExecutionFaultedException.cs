namespace ViciOne.ServiceBus
{
    using System;


    [Serializable]
    public class ActivityExecutionFaultedException :
        ActivityExecutionException
    {
        public ActivityExecutionFaultedException()
            : this("The routing slip activity execution faulted with an unspecified exception")
        {
        }

        public ActivityExecutionFaultedException(string message)
            : base(message)
        {
        }

        public ActivityExecutionFaultedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
