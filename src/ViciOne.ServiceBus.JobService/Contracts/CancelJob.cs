using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Requests cancellation of a submitted job.</summary>
public interface CancelJob
{
    /// <summary>Gets the identifier of the job to cancel.</summary>
    Guid JobId { get; }

    /// <summary>Gets the cancellation reason, or <see langword="null" /> to use the domain default.</summary>
    string? Reason { get; }
}
