using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the command for set job progress.</summary>
public class SetJobProgressCommand :
    SetJobProgress
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
    /// <summary>Gets or sets the sequence number.</summary>
    public long SequenceNumber { get; set; }
    /// <summary>Gets or sets the value.</summary>
    public long Value { get; set; }
    /// <summary>Gets or sets the limit.</summary>
    public long? Limit { get; set; }
}
