using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for job context.
/// </summary>
public interface JobContext :
    PipeContext,
    MessageContext,
    ISendEndpointProvider,
    IPublishEndpoint
{
    /// <summary>
    /// Gets the job id value.
    /// </summary>
    Guid JobId { get; }
    /// <summary>
    /// Gets the attempt id value.
    /// </summary>
    Guid AttemptId { get; }

    /// <summary>
    /// If previously attempted, this value is > 0
    /// </summary>
    int RetryAttempt { get; }

    /// <summary>
    /// The last reported progress value for this job
    /// </summary>
    long? LastProgressValue { get; }

    /// <summary>
    /// The last reported progress limit for this job
    /// </summary>
    long? LastProgressLimit { get; }

    /// <summary>
    /// How long the job has been running
    /// </summary>
    TimeSpan ElapsedTime { get; }

    /// <summary>
    /// The job properties that were supplied when the job was submitted
    /// </summary>
    IPropertyCollection JobProperties { get; }

    /// <summary>
    /// Properties that were configured for this job type
    /// </summary>
    IPropertyCollection JobTypeProperties { get; }

    /// <summary>
    /// Properties that were configured for this job consumer instance
    /// </summary>
    IPropertyCollection InstanceProperties { get; }

    /// <summary>
    /// Sets the job's progress, which gets reported back to the job saga
    /// </summary>
    /// <param name="value"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SetJobProgressAsync(long value, long? limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save job state, typically when canceling or faulting, so that subsequent retries can resume from the saved state
    /// </summary>
    /// <param name="jobState"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SaveJobStateAsync<T>(T? jobState, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Attempts to get job state.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="jobState">The job state value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetJobState<T>([NotNullWhen(true)] out T? jobState)
        where T : class;
}


/// <summary>
/// Defines the contract for job context.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface JobContext<out TMessage> :
    JobContext
    where TMessage : class
{
    /// <summary>
    /// The message that initiated the job
    /// </summary>
    TMessage Job { get; }
}
