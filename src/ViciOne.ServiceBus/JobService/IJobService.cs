using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Defines the contract for job service.
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Gets the instance address value.
    /// </summary>
    Uri InstanceAddress { get; }

    /// <summary>
    /// Gets the settings value.
    /// </summary>
    JobServiceSettings Settings { get; }

    /// <summary>
    /// Starts a job
    /// </summary>
    /// <typeparam name="T">The message type that is used to initiate the job</typeparam>
    /// <param name="context">The context of the message being consumed</param>
    /// <param name="job">The job command</param>
    /// <param name="jobPipe">The pipe which executes the job</param>
    /// <param name="jobOptions">The job options</param>
    /// <returns>The newly created job's handle</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task StartJobAsync<T>(ConsumeContext<StartJob> context, T job, IPipe<ConsumeContext<T>> jobPipe, JobOptions<T> jobOptions, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Shut town the job service, cancelling any pending jobs
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task StopAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get job.
    /// </summary>
    /// <param name="jobId">The job id value.</param>
    /// <param name="jobReference">The job reference value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobReference);

    /// <summary>
    /// Remove the job from the roster
    /// </summary>
    /// <param name="jobId"></param>
    /// <param name="jobHandle"></param>
    bool TryRemoveJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobHandle);

    /// <summary>
    /// Registers a job type at bus configuration time so that the options can be announced when the bus is started/stopped
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="options"></param>
    /// <param name="jobTypeId"></param>
    /// <param name="jobTypeName"></param>
    /// <typeparam name="T"></typeparam>
    void RegisterJobType<T>(IReceiveEndpointConfigurator configurator, JobOptions<T> options, Guid jobTypeId, string jobTypeName)
        where T : class;

    /// <summary>
    /// Performs the bus started operation.
    /// </summary>
    /// <param name="publishEndpoint">The publish endpoint value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task BusStartedAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return the registered JobTypeId for the job type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Guid GetJobTypeId<T>()
        where T : class;

    /// <summary>
    /// Configures supervise job consumer.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    void ConfigureSuperviseJobConsumer(IReceiveEndpointConfigurator configurator);
}
