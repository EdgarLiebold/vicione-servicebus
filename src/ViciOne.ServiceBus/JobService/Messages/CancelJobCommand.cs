#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


public class CancelJobCommand :
    CancelJob
{
    public Guid JobId { get; set; }
    public string? Reason { get; set; }
}
