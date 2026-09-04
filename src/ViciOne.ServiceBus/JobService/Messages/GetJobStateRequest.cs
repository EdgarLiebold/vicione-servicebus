using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class GetJobStateRequest :
    GetJobState
{
    public Guid JobId { get; set; }
}
