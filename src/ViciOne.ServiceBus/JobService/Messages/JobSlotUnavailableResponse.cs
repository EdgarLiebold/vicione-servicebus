#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


public class JobSlotUnavailableResponse :
    JobSlotUnavailable
{
    public Guid JobId { get; set; }
}
