using System;

namespace ViciOne.ServiceBus.Contracts.JobService;

/// <summary>Reports that a service instance started processing a job.</summary>
public interface JobStarted
{
    /// <summary>Gets the identifier of the started job.</summary>
    Guid JobId { get; }

    /// <summary>Gets the execution attempt that started.</summary>
    Guid AttemptId { get; }

    /// <summary>Gets the zero-based attempt number.</summary>
    int RetryAttempt { get; }

    /// <summary>Gets the execution start instant.</summary>
    DateTimeOffset Timestamp { get; }
}


/// <summary>Reports that a service instance started processing a strongly typed job.</summary>
/// <typeparam name="TJob">The submitted job type.</typeparam>
public interface JobStarted<out TJob> :
    JobStarted
    where TJob : class
{
    /// <summary>Gets the submitted job payload.</summary>
    TJob Job { get; }
}
