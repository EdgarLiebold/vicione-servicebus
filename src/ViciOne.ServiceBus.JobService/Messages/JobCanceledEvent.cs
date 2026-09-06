using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable terminal outcome emitted when a job is canceled.</summary>
internal sealed class JobCanceledEvent :
    JobCanceled
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? Reason { get; set; }
}
