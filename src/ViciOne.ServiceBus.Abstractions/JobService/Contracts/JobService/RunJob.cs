using System;

namespace ViciOne.ServiceBus.Contracts.JobService;
/// <summary>
/// Run a scheduled job immediately, vs waiting for the next scheduled job time
/// </summary>
public interface RunJob
{
    Guid JobId { get; }
}
