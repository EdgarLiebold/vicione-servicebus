using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobAttemptCanceled
{
    Guid JobId { get; }
    Guid AttemptId { get; }
    DateTime Timestamp { get; }
    string Reason { get; }
}
