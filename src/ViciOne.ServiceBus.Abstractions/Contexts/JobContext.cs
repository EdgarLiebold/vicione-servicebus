using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for job operations.</summary>
public interface JobContext :
    PipeContext,
    MessageContext,
    ISendEndpointProvider,
    IPublishEndpoint
{
    /// <summary>Gets the job id.</summary>
    Guid JobId { get; }
    /// <summary>Gets the attempt id.</summary>
    Guid AttemptId { get; }

    /// <summary>If previously attempted, this value is > 0.</summary>
    int RetryAttempt { get; }

    /// <summary>The last reported progress value for this job.</summary>
    long? LastProgressValue { get; }

    /// <summary>The last reported progress limit for this job.</summary>
    long? LastProgressLimit { get; }

    /// <summary>How long the job has been running.</summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>The job properties that were supplied when the job was submitted.</summary>
    IPropertyCollection JobProperties { get; }

    /// <summary>Properties that were configured for this job type.</summary>
    IPropertyCollection JobTypeProperties { get; }

    /// <summary>Properties that were configured for this job consumer instance.</summary>
    IPropertyCollection InstanceProperties { get; }

    /// <summary>Sets the job's progress, which gets reported back to the job saga.</summary>
    /// <param name="value">The value to process.</param>
    /// <param name="limit">The limit.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetJobProgressAsync(long value, long? limit, CancellationToken cancellationToken = default);

    /// <summary>Save job state, typically when canceling or faulting, so that subsequent retries can resume from the saved state.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="jobState">The job state.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveJobStateAsync<T>(T? jobState, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Attempts to get job state.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="jobState">Receives the job state produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetJobState<T>([NotNullWhen(true)] out T? jobState)
        where T : class;
}


/// <summary>Exposes state for job operations.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface JobContext<out TMessage> :
    JobContext
    where TMessage : class
{
    /// <summary>The message that initiated the job.</summary>
    TMessage Job { get; }
}
