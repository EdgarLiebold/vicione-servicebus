using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Reports that no eligible execution slot is currently available.</summary>
public interface JobSlotUnavailable
{
    /// <summary>Gets the job for which no slot was available.</summary>
    Guid JobId { get; }
}
