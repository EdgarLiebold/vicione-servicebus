using System;
using ViciOne.ServiceBus.Contracts.JobService;

#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

public class SetJobProgressCommand :
    SetJobProgress
{
    public Guid JobId { get; set; }
    public Guid AttemptId { get; set; }
    public long SequenceNumber { get; set; }
    public long Value { get; set; }
    public long? Limit { get; set; }
}
