using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable command used to submit a typed job.</summary>
/// <typeparam name="TJob">The job contract type.</typeparam>
internal sealed class SubmitJobCommand<TJob> :
    SubmitJob<TJob>
    where TJob : class
{
    /// <summary>Gets or sets the stable job identifier.</summary>
    public Guid JobId { get; set; }

    /// <summary>Gets or sets the job payload.</summary>
    public TJob Job { get; set; } = null!;

    /// <summary>Gets or sets the optional one-time or recurring execution schedule.</summary>
    public JobSchedule? Schedule { get; set; }

    /// <summary>Gets or sets the optional job metadata.</summary>
    public IReadOnlyDictionary<string, object>? JobProperties { get; set; }
}
