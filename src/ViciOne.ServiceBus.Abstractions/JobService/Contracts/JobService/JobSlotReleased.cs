using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobSlotReleased
{
    Guid JobTypeId { get; }

    Guid JobId { get; }

    JobSlotDisposition Disposition { get; }
}
