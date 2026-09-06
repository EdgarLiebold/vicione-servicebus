using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Defines the operations required by job attempt canceled.</summary>
public interface JobAttemptCanceled
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }
    /// <summary>Gets the attempt id.</summary>
    Guid AttemptId { get; }
    /// <summary>Gets the timestamp.</summary>
    DateTimeOffset Timestamp { get; }
    /// <summary>Gets the reason.</summary>
    string Reason { get; }
}
