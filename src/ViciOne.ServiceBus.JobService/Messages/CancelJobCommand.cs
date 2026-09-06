using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable request used to cancel a job and its active attempt.</summary>
internal sealed class CancelJobCommand :
    CancelJob
{
    public Guid JobId { get; set; }
    public string? Reason { get; set; }
}
