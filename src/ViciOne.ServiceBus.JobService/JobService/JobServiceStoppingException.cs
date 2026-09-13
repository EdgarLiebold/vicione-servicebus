using System;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Indicates that a job was rejected because the local job runtime is stopping.</summary>
/// <param name="jobId">The identifier of the rejected job.</param>
public sealed class JobServiceStoppingException(Guid jobId) :
    ViciOneServiceBusException($"The job service is stopping, job cannot be started: {jobId}")
{
    /// <summary>Gets the identifier of the job rejected during shutdown.</summary>
    public Guid JobId { get; } = jobId;
}
