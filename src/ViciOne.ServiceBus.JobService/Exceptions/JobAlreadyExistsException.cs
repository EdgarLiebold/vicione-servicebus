using System;

namespace ViciOne.ServiceBus;

/// <summary>Indicates that the local runtime already tracks the specified job identifier.</summary>
public sealed class JobAlreadyExistsException :
    ViciOneServiceBusException
{
    /// <summary>Initializes the exception for the duplicate job identifier.</summary>
    /// <param name="jobId">The identifier already tracked by the local runtime.</param>
    public JobAlreadyExistsException(Guid jobId)
        : base($"The job already exists in the roster: {jobId}")
    {
        JobId = jobId;
    }

    /// <summary>Gets the identifier already tracked by the local runtime.</summary>
    public Guid JobId { get; }
}
