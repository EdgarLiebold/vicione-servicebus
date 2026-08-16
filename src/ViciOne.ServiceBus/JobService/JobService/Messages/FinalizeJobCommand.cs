#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


public class FinalizeJobCommand :
    FinalizeJob
{
    public Guid JobId { get; set; }
}
