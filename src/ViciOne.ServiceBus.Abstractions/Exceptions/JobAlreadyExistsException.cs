using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to job already exists.
/// </summary>
public class JobAlreadyExistsException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public JobAlreadyExistsException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="jobId">The job id value.</param>
    public JobAlreadyExistsException(Guid jobId)
        : base($"The job already exists in the roster: {jobId}")
    {
    }
}
