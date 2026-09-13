using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Coordinates the jobs executing within one service instance.</summary>
internal interface IJobService
{
    /// <summary>Gets the endpoint address that identifies this job-service instance.</summary>
    Uri InstanceAddress { get; }

    /// <summary>Gets the immutable runtime settings for this job-service instance.</summary>
    IJobServiceSettings Settings { get; }

    /// <summary>Admits and starts one local execution of a submitted job.</summary>
    /// <typeparam name="TJob">The submitted job contract.</typeparam>
    /// <param name="context">The start command and attempt metadata.</param>
    /// <param name="job">The deserialized job.</param>
    /// <param name="jobPipe">The consumer pipeline that executes the job.</param>
    /// <param name="jobOptions">The registered options for the job type.</param>
    /// <param name="cancellationToken">The token that cancels admission and startup.</param>
    /// <returns>A task that completes after the job has been admitted and started.</returns>
    Task StartJobAsync<TJob>(ConsumeContext<IStartJob> context, TJob job, IPipe<ConsumeContext<TJob>> jobPipe, JobOptions<TJob> jobOptions,
        CancellationToken cancellationToken = default)
        where TJob : class;

    /// <summary>Stops the job service and cancels its active jobs.</summary>
    /// <param name="publishEndpoint">The endpoint used to withdraw this instance from job distribution.</param>
    /// <param name="cancellationToken">The token that cancels shutdown.</param>
    /// <returns>A task that completes after active jobs and the heartbeat loop have stopped.</returns>
    Task StopAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default);

    /// <summary>Attempts to get an active local job.</summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="jobReference">Receives the active job handle when found.</param>
    /// <returns><see langword="true" /> when the job is active locally; otherwise, <see langword="false" />.</returns>
    bool TryGetJob(Guid jobId, [NotNullWhen(true)] out IJobHandle? jobReference);

    /// <summary>Removes an active local job from the runtime roster.</summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="jobHandle">Receives the removed job handle when found.</param>
    /// <returns><see langword="true" /> when the job was removed; otherwise, <see langword="false" />.</returns>
    bool TryRemoveJob(Guid jobId, [NotNullWhen(true)] out IJobHandle? jobHandle);

    /// <summary>Registers the metadata announced for a job type while the bus is running.</summary>
    /// <typeparam name="TJob">The job contract type.</typeparam>
    /// <param name="options">The configured options for the job type.</param>
    /// <param name="jobTypeId">The stable job-type identifier.</param>
    /// <param name="jobTypeName">The diagnostic job-type name.</param>
    void RegisterJobType<TJob>(JobOptions<TJob> options, Guid jobTypeId, string jobTypeName)
        where TJob : class;

    /// <summary>Starts instance announcements and heartbeat publication after the bus is ready.</summary>
    /// <param name="publishEndpoint">The endpoint used to publish instance and job-type availability.</param>
    /// <param name="cancellationToken">The token that cancels startup.</param>
    /// <returns>A task that completes after the initial availability announcements have been published.</returns>
    Task BusStartedAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default);

    /// <summary>Gets the stable identifier registered for a job contract.</summary>
    /// <typeparam name="TJob">The job contract type.</typeparam>
    /// <returns>The registered job-type identifier.</returns>
    Guid GetJobTypeId<TJob>()
        where TJob : class;

    /// <summary>Connects the local cancellation and liveness supervisor to a receive endpoint.</summary>
    /// <param name="configurator">The receive endpoint to configure.</param>
    void ConfigureSuperviseJobConsumer(IReceiveEndpointConfigurator configurator);
}
