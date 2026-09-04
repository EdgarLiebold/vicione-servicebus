using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobSubmissionAccepted
{
    Guid JobId { get; }
}
