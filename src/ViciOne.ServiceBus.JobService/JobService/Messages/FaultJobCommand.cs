using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable failure record produced by an unsuccessful job attempt.</summary>
internal sealed class FaultJobCommand :
    IFaultJob
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public int RetryAttempt { get; set; }
    public TimeSpan? Duration { get; set; }
    public ExceptionInfo Exceptions { get; set; } = null!;
    public IReadOnlyDictionary<string, object> Job { get; set; } = null!;
    public Guid JobTypeId { get; set; }
}
