// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus;

using System;
using System.Runtime.Serialization;


[Serializable]
public class JobServiceStoppingException :
    ViciOneServiceBusException
{
    public JobServiceStoppingException()
    {
    }

    public JobServiceStoppingException(Guid jobId)
        : base($"The job service is stopping, job cannot be started: {jobId}")
    {
    }

#if NET8_0_OR_GREATER
    [Obsolete("Formatter-based serialization is obsolete and should not be used.")]
#endif
    protected JobServiceStoppingException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
