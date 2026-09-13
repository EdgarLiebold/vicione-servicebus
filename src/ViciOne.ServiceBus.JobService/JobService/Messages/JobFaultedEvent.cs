using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serialized job payload and exception details emitted after terminal failure.</summary>
internal sealed class JobFaultedEvent :
    IJobFaulted
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public TimeSpan? Duration { get; set; }
    public IReadOnlyDictionary<string, object> Job { get; set; } = null!;
    public ExceptionInfo Exceptions { get; set; } = null!;
}
