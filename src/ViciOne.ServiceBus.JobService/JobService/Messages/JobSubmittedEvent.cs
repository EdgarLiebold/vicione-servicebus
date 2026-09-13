using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable event that starts durable coordination of a submitted job.</summary>
internal sealed class JobSubmittedEvent :
    IJobSubmitted
{
    /// <summary>Gets or sets the stable job identifier.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the stable identity of the registered job type and endpoint.</summary>
    public Guid JobTypeId { get; set; }
    /// <summary>Gets or sets the submission instant.</summary>
    public DateTimeOffset Timestamp { get; set; }
    /// <summary>Gets or sets the maximum duration of one execution attempt.</summary>
    public TimeSpan JobTimeout { get; set; }
    /// <summary>Gets or sets the serialized job payload.</summary>
    public IReadOnlyDictionary<string, object> Job { get; set; } = null!;
    /// <summary>Gets or sets the optional job metadata.</summary>
    public IReadOnlyDictionary<string, object>? JobProperties { get; set; }
    /// <summary>Gets or sets the optional one-time or recurring execution schedule.</summary>
    public IJobSchedule? Schedule { get; set; }
}
