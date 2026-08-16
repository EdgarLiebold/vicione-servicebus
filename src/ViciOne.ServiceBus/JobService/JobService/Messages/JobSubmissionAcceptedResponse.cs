#nullable enable
namespace ViciOne.ServiceBus.JobService.Messages;

using System;
using Contracts.JobService;


public class JobSubmissionAcceptedResponse :
    JobSubmissionAccepted
{
    public Guid JobId { get; set; }
}
