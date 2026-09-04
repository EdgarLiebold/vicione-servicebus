using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class JobCanceledEvent :
    JobCanceled
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? Reason { get; set; }
}
