// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


public class SetJobProgressCommand :
    SetJobProgress
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public long SequenceNumber { get; set; }
    public long Value { get; set; }
    public long? Limit { get; set; }
}
