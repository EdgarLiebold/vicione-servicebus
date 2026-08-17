namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.Serialization;


    [Serializable]
    public class JobAlreadyExistsException :
        ViciOneServiceBusException
    {
        public JobAlreadyExistsException()
        {
        }

        public JobAlreadyExistsException(Guid jobId)
            : base($"The job already exists in the roster: {jobId}")
        {
        }

        [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
        protected JobAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
