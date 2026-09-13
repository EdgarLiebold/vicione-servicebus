using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Reports that a job reached its canceled terminal state.</summary>
public interface IJobCanceled
{
    /// <summary>Gets the identifier of the canceled job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the cancellation instant.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the cancellation reason, when one was provided.</summary>
    string? Reason { get; }
}
