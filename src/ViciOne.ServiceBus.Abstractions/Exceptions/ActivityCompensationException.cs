namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class ActivityCompensationException :
        CourierException
    {
        public ActivityCompensationException()
        {
        }

        public ActivityCompensationException(string message)
            : base(message)
        {
        }

        public ActivityCompensationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected ActivityCompensationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
