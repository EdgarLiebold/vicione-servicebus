using System;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Indicates that the local runtime already tracks the specified job identifier.</summary>
/// <param name="jobId">The identifier already tracked by the local runtime.</param>
public sealed class JobAlreadyExistsException(Guid jobId) :
    ViciOneServiceBusException($"The job already exists in the roster: {jobId}")
{
    /// <summary>Gets the identifier already tracked by the local runtime.</summary>
    public Guid JobId { get; } = jobId;
}
