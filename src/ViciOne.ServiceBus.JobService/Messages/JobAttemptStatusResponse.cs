using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable status reported for a specific execution attempt.</summary>
internal sealed class JobAttemptStatusResponse :
    JobAttemptStatus
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public JobAttemptStatusKind Status { get; set; }
}
