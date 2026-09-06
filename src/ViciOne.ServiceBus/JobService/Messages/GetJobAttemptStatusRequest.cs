using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Carries the request for get job attempt status.</summary>
public class GetJobAttemptStatusRequest :
    GetJobAttemptStatus
{
    /// <summary>Gets or sets the job id.</summary>
    public Guid JobId { get; set; }
    /// <summary>Gets or sets the attempt id.</summary>
    public Guid AttemptId { get; set; }
}
