using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobRetryDelayElapsed
{
    Guid JobId { get; }
}
