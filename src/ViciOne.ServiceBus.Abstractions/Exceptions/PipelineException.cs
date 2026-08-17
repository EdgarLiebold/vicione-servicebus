namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class PipelineException :
        ViciOneServiceBusException
    {
        public PipelineException()
        {
        }

        public PipelineException(string message)
            : base(message)
        {
        }

        public PipelineException(string message, Exception innerException)
            :
            base(message, innerException)
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected PipelineException(SerializationInfo info, StreamingContext context)
            :
            base(info, context)
        {
        }
    }
}
