using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Reports that an execution attempt acknowledged cancellation.</summary>
public interface JobAttemptCanceled
{
    /// <summary>Gets the identifier of the owning job.</summary>
    Guid JobId { get; }
    /// <summary>Gets the canceled execution attempt.</summary>
    Guid AttemptId { get; }
    /// <summary>Gets the cancellation instant.</summary>
    DateTimeOffset Timestamp { get; }
    /// <summary>Gets the reason acknowledged by the job consumer.</summary>
    string Reason { get; }
}
