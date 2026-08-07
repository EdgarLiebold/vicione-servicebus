// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService;

using System;


/// <summary>
/// Run a scheduled job immediately, vs waiting for the next scheduled job time
/// </summary>
public interface RunJob
{
    Guid JobId { get; }
}
