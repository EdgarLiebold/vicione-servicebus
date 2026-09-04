using System;

namespace ViciOne.ServiceBus;

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
}
