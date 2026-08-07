// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Contracts.JobService
{
    using System;


    public interface JobAttemptCanceled
    {
        Guid JobId { get; }
        Guid AttemptId { get; }
        DateTime Timestamp { get; }
        string Reason { get; }
    }
}
