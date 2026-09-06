using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to job service stopping.</summary>
public class JobServiceStoppingException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public JobServiceStoppingException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="jobId">The job id.</param>
    public JobServiceStoppingException(Guid jobId)
        : base($"The job service is stopping, job cannot be started: {jobId}")
    {
    }
}
