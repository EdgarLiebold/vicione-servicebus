using System;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService.Messages;

/// <summary>Provides the serializable request that makes a scheduled job eligible for execution.</summary>
internal sealed class RunJobCommand :
    IRunJob
{
    public Guid JobId { get; set; }
}
