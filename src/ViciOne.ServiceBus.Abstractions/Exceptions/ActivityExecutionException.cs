namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class ActivityExecutionException :
        CourierException
    {
        public ActivityExecutionException()
        {
        }

        public ActivityExecutionException(string message)
            : base(message)
        {
        }

        public ActivityExecutionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected ActivityExecutionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
