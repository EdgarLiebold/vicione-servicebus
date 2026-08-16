#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


public class RunJobCommand :
    RunJob
{
    public Guid JobId { get; set; }
}
