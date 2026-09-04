using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

public interface JobAttemptCanceled
{
    Guid JobId { get; }
    Guid AttemptId { get; }
    DateTimeOffset Timestamp { get; }
    string Reason { get; }
}
