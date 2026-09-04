using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobSlotUnavailable
{
    Guid JobId { get; }
}
