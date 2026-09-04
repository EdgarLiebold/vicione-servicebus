using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobSlotWaitElapsed
{
    Guid JobId { get; }
}
