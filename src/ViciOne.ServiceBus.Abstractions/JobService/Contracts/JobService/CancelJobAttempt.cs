using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by cancel job attempt.</summary>
[ConfigureConsumeTopology(false)]
public interface CancelJobAttempt
{
    /// <summary>The job identifier.</summary>
    Guid JobId { get; }

    /// <summary>Identifies this attempt to run the job.</summary>
    Guid AttemptId { get; }

    /// <summary>The supplied reason for the job cancellation.</summary>
    string? Reason { get; }
}
