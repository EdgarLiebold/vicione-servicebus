using System;

namespace ViciOne.ServiceBus;

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
}
