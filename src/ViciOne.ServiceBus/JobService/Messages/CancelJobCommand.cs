using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class CancelJobCommand :
    CancelJob
{
    public Guid JobId { get; set; }
    public string? Reason { get; set; }
}
