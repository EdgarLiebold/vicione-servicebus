using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>
/// Provides a set job progress command implementation.
/// </summary>
public class SetJobProgressCommand :
    SetJobProgress
{
    /// <summary>
    /// Gets or sets the job id value.
    /// </summary>
    public Guid JobId { get; set; }
    /// <summary>
    /// Gets or sets the attempt id value.
    /// </summary>
    public Guid AttemptId { get; set; }
    /// <summary>
    /// Gets or sets the sequence number value.
    /// </summary>
    public long SequenceNumber { get; set; }
    /// <summary>
    /// Gets or sets the underlying value.
    /// </summary>
    public long Value { get; set; }
    /// <summary>
    /// Gets or sets the limit value.
    /// </summary>
    public long? Limit { get; set; }
}
