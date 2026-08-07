// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using System.Collections.Generic;
using Contracts.JobService;


public class SubmitJobCommand<T> :
    SubmitJob<T>
    where T : class
{
    public Guid JobId { get; set; }
    public T Job { get; set; } = null!;
    public RecurringJobSchedule? Schedule { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}
