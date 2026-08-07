// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService;

using System;
using System.Collections.Generic;


public interface SubmitJob<out TJob>
    where TJob : class
{
    Guid JobId { get; }

    TJob Job { get; }

    RecurringJobSchedule? Schedule { get; }

    Dictionary<string, object>? Properties { get; }
}
