using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to job service stopping.
/// </summary>
[Serializable]
public class JobServiceStoppingException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public JobServiceStoppingException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="jobId">The job id value.</param>
    public JobServiceStoppingException(Guid jobId)
        : base($"The job service is stopping, job cannot be started: {jobId}")
    {
    }
}
