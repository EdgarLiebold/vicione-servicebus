using System;

namespace ViciOne.ServiceBus;

/// <summary>Indicates that a job was rejected because the local job runtime is stopping.</summary>
public sealed class JobServiceStoppingException :
    ViciOneServiceBusException
{
    /// <summary>Initializes the exception for the job rejected during shutdown.</summary>
    /// <param name="jobId">The identifier of the rejected job.</param>
    public JobServiceStoppingException(Guid jobId)
        : base($"The job service is stopping, job cannot be started: {jobId}")
    {
        JobId = jobId;
    }

    /// <summary>Gets the identifier of the job rejected during shutdown.</summary>
    public Guid JobId { get; }
}
