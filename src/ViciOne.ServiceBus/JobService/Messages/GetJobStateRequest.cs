using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the request for get job state.</summary>
public class GetJobStateRequest :
    GetJobState
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
}
