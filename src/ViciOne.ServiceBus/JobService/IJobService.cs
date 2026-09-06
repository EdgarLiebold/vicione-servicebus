using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Defines the operations required by job service.</summary>
public interface IJobService
{
    /// <summary>Gets the instance address.</summary>
    Uri InstanceAddress { get; }

    /// <summary>Gets the settings.</summary>
    JobServiceSettings Settings { get; }

    /// <summary>Starts a job.</summary>
    /// <typeparam name="T">The message type that is used to initiate the job.</typeparam>
    /// <param name="context">The context of the message being consumed.</param>
    /// <param name="job">The job command.</param>
    /// <param name="jobPipe">The pipe which executes the job.</param>
    /// <param name="jobOptions">The job options.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The newly created job's handle.</returns>
    Task StartJobAsync<T>(ConsumeContext<StartJob> context, T job, IPipe<ConsumeContext<T>> jobPipe, JobOptions<T> jobOptions, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Shut town the job service, cancelling any pending jobs.</summary>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task StopAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default);

    /// <summary>Attempts to get job.</summary>
    /// <param name="jobId">The job id.</param>
    /// <param name="jobReference">Receives the job reference produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobReference);

    /// <summary>Remove the job from the roster.</summary>
    /// <param name="jobId">The job id.</param>
    /// <param name="jobHandle">Receives the job handle produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryRemoveJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobHandle);

    /// <summary>Registers a job type at bus configuration time so that the options can be announced when the bus is started/stopped.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="jobTypeId">The job type id.</param>
    /// <param name="jobTypeName">The job type name.</param>
    void RegisterJobType<T>(IReceiveEndpointConfigurator configurator, JobOptions<T> options, Guid jobTypeId, string jobTypeName)
        where T : class;

    /// <summary>Notifies the component that the bus has started.</summary>
    /// <param name="publishEndpoint">The publish endpoint.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task BusStartedAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default);

    /// <summary>Return the registered JobTypeId for the job type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The job type id.</returns>
    Guid GetJobTypeId<T>()
        where T : class;

    /// <summary>Configures supervise job consumer.</summary>
    /// <param name="configurator">The configurator to update.</param>
    void ConfigureSuperviseJobConsumer(IReceiveEndpointConfigurator configurator);
}
