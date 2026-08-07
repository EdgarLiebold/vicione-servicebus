// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService
{
    using System;


    public interface JobSubmissionAccepted
    {
        Guid JobId { get; }
    }
}
