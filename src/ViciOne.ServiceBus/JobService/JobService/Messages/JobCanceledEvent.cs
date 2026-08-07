// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


public class JobCanceledEvent :
    JobCanceled
{
    public Guid JobId { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Reason { get; set; }
}
