using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable acknowledgement for a durably accepted job submission.</summary>
internal sealed class JobSubmissionAcceptedResponse :
    IJobSubmissionAccepted
{
    public Guid JobId { get; set; }
}
