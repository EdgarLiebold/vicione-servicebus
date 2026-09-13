using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Clients.Requests;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Provides job submission, state query, cancellation, retry, and finalization operations.</summary>
public static class JobServiceExtensions
{
    /// <summary>Returns the current lifecycle state of a job.</summary>
    /// <param name="client">The request client connected to the job state endpoint.</param>
    /// <param name="jobId">The identifier of the job to query.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The current job state.</returns>
    public static async Task<IJobState> GetJobStateAsync(
        this IRequestClient<IGetJobState> client,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ValidateJobId(jobId);

        Response<IJobState> response = await client
            .GetResponseAsync<IJobState>(new GetJobStateRequest { JobId = jobId }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return response.Message;
    }

    /// <summary>Returns the current lifecycle state and deserialized checkpoint of a job.</summary>
    /// <typeparam name="TCheckpoint">The checkpoint type stored by the job.</typeparam>
    /// <param name="client">The request client connected to the job state endpoint.</param>
    /// <param name="jobId">The identifier of the job to query.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The current job state and its optional checkpoint.</returns>
    public static async Task<IJobState<TCheckpoint>> GetJobStateAsync<TCheckpoint>(
        this IRequestClient<IGetJobState> client,
        Guid jobId,
        CancellationToken cancellationToken = default)
        where TCheckpoint : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ValidateJobId(jobId);

        Response<IJobState> response = await client
            .GetResponseAsync<IJobState>(new GetJobStateRequest { JobId = jobId }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (response is MessageResponse<IJobState> messageResponse)
        {
            TCheckpoint? checkpoint = response.Message.Checkpoint is null
                ? null
                : messageResponse.DeserializeObject<TCheckpoint>(response.Message.Checkpoint);

            return new JobStateResponse<TCheckpoint>(response.Message, checkpoint);
        }

        return new JobStateResponse<TCheckpoint>(response.Message);
    }

    /// <summary>Publishes a job with a generated identifier.</summary>
    /// <typeparam name="TJob">The job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="job">The job to submit.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The generated job identifier.</returns>
    public static Task<Guid> SubmitJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        TJob job,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        return SubmitJobAsync(publishEndpoint, NewId.NextGuid(), job, null, cancellationToken);
    }

    /// <summary>Publishes a job with a generated identifier and application-defined properties.</summary>
    /// <typeparam name="TJob">The job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="job">The job to submit.</param>
    /// <param name="setJobProperties">The callback that adds job properties.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The generated job identifier.</returns>
    public static Task<Guid> SubmitJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        TJob job,
        Action<ISetPropertyCollection> setJobProperties,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(setJobProperties);
        return SubmitJobAsync(publishEndpoint, NewId.NextGuid(), job, setJobProperties, cancellationToken);
    }

    /// <summary>Publishes a job with an explicit identifier and optional application-defined properties.</summary>
    /// <typeparam name="TJob">The job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="jobId">The identifier assigned to the job.</param>
    /// <param name="job">The job to submit.</param>
    /// <param name="setJobProperties">The optional callback that adds job properties.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The supplied job identifier.</returns>
    public static async Task<Guid> SubmitJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        Guid jobId,
        TJob job,
        Action<ISetPropertyCollection>? setJobProperties = null,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobId(jobId);

        await publishEndpoint
            .PublishAsync<ISubmitJob<TJob>>(CreateSubmitJobCommand(jobId, job, setJobProperties), cancellationToken)
            .ConfigureAwait(false);

        return jobId;
    }

    /// <summary>Initializes and publishes a job from property values with a generated identifier.</summary>
    /// <typeparam name="TJob">The job contract type to initialize.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="cancellationToken">The token that cancels initialization or publication.</param>
    /// <returns>The generated job identifier.</returns>
    public static Task<Guid> SubmitJobFromValuesAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        object values,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        return SubmitJobFromValuesAsync<TJob>(publishEndpoint, NewId.NextGuid(), values, null, cancellationToken);
    }

    /// <summary>Initializes and publishes a job from property values with application-defined properties.</summary>
    /// <typeparam name="TJob">The job contract type to initialize.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="setJobProperties">The callback that adds job properties.</param>
    /// <param name="cancellationToken">The token that cancels initialization or publication.</param>
    /// <returns>The generated job identifier.</returns>
    public static Task<Guid> SubmitJobFromValuesAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        object values,
        Action<ISetPropertyCollection> setJobProperties,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(setJobProperties);
        return SubmitJobFromValuesAsync<TJob>(publishEndpoint, NewId.NextGuid(), values, setJobProperties, cancellationToken);
    }

    /// <summary>Initializes and publishes a job from property values with an explicit identifier.</summary>
    /// <typeparam name="TJob">The job contract type to initialize.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="jobId">The identifier assigned to the job.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="setJobProperties">The optional callback that adds job properties.</param>
    /// <param name="cancellationToken">The token that cancels initialization or publication.</param>
    /// <returns>The supplied job identifier.</returns>
    public static async Task<Guid> SubmitJobFromValuesAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        Guid jobId,
        object values,
        Action<ISetPropertyCollection>? setJobProperties = null,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(values);
        ValidateJobId(jobId);

        InitializeContext<TJob> context = await MessageInitializerCache<TJob>
            .InitializeAsync(values, cancellationToken)
            .ConfigureAwait(false);

        await publishEndpoint
            .PublishAsync<ISubmitJob<TJob>>(CreateSubmitJobCommand(jobId, context.Message, setJobProperties), cancellationToken)
            .ConfigureAwait(false);

        return jobId;
    }

    /// <summary>Submits a job request with a generated identifier and returns the accepted identifier.</summary>
    /// <typeparam name="TJob">The job contract type.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="job">The job to submit.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static Task<Guid> SubmitJobAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        TJob job,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        return SubmitJobAsync(client, NewId.NextGuid(), job, null, cancellationToken);
    }

    /// <summary>Submits a job request with a generated identifier and application-defined properties.</summary>
    /// <typeparam name="TJob">The job contract type.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="job">The job to submit.</param>
    /// <param name="setJobProperties">The callback that adds job properties.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static Task<Guid> SubmitJobAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        TJob job,
        Action<ISetPropertyCollection> setJobProperties,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(setJobProperties);
        return SubmitJobAsync(client, NewId.NextGuid(), job, setJobProperties, cancellationToken);
    }

    /// <summary>Submits a job request with an explicit identifier and optional application-defined properties.</summary>
    /// <typeparam name="TJob">The job contract type.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="jobId">The identifier assigned to the job.</param>
    /// <param name="job">The job to submit.</param>
    /// <param name="setJobProperties">The optional callback that adds job properties.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static async Task<Guid> SubmitJobAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        Guid jobId,
        TJob job,
        Action<ISetPropertyCollection>? setJobProperties = null,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobId(jobId);

        Response<IJobSubmissionAccepted> response = await client
            .GetResponseAsync<IJobSubmissionAccepted>(CreateSubmitJobCommand(jobId, job, setJobProperties), cancellationToken)
            .ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>Initializes and submits a job request from property values with a generated identifier.</summary>
    /// <typeparam name="TJob">The job contract type to initialize.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static Task<Guid> SubmitJobFromValuesAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        object values,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        return SubmitJobFromValuesAsync<TJob>(client, NewId.NextGuid(), values, null, cancellationToken);
    }

    /// <summary>Initializes and submits a job request from property values with application-defined properties.</summary>
    /// <typeparam name="TJob">The job contract type to initialize.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="setJobProperties">The callback that adds job properties.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static Task<Guid> SubmitJobFromValuesAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        object values,
        Action<ISetPropertyCollection> setJobProperties,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(setJobProperties);
        return SubmitJobFromValuesAsync<TJob>(client, NewId.NextGuid(), values, setJobProperties, cancellationToken);
    }

    /// <summary>Initializes and submits a job request from property values with an explicit identifier.</summary>
    /// <typeparam name="TJob">The job contract type to initialize.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="jobId">The identifier assigned to the job.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="setJobProperties">The optional callback that adds job properties.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static async Task<Guid> SubmitJobFromValuesAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        Guid jobId,
        object values,
        Action<ISetPropertyCollection>? setJobProperties = null,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(values);
        ValidateJobId(jobId);

        InitializeContext<TJob> context = await MessageInitializerCache<TJob>
            .InitializeAsync(values, cancellationToken)
            .ConfigureAwait(false);

        Response<IJobSubmissionAccepted> response = await client
            .GetResponseAsync<IJobSubmissionAccepted>(CreateSubmitJobCommand(jobId, context.Message, setJobProperties), cancellationToken)
            .ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>Submits a job directly to its job-consumer request endpoint.</summary>
    /// <typeparam name="TJob">The job contract type.</typeparam>
    /// <param name="client">The request client connected to the job-consumer endpoint.</param>
    /// <param name="job">The job to submit.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static async Task<Guid> SubmitJobAsync<TJob>(
        this IRequestClient<TJob> client,
        TJob job,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(job);

        Response<IJobSubmissionAccepted> response = await client
            .GetResponseAsync<IJobSubmissionAccepted>(job, cancellationToken)
            .ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>Initializes and submits a job directly to its job-consumer request endpoint.</summary>
    /// <typeparam name="TJob">The job contract type to initialize.</typeparam>
    /// <param name="client">The request client connected to the job-consumer endpoint.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static async Task<Guid> SubmitJobFromValuesAsync<TJob>(
        this IRequestClient<TJob> client,
        object values,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(values);

        Response<IJobSubmissionAccepted> response = await client.Advanced()
            .GetResponseAsync<IJobSubmissionAccepted>(values, timeout: default, cancellationToken)
            .ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>Requests cancellation of a job that is in a cancellable state.</summary>
    /// <param name="publishEndpoint">The endpoint used to publish the cancellation command.</param>
    /// <param name="jobId">The identifier of the job to cancel.</param>
    /// <param name="reason">The optional application reason recorded with the cancellation.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the command has been published.</returns>
    public static Task CancelJobAsync(
        this IPublishEndpoint publishEndpoint,
        Guid jobId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ValidateJobId(jobId);

        return publishEndpoint.PublishAsync<ICancelJob>(new CancelJobCommand
        {
            JobId = jobId,
            Reason = string.IsNullOrWhiteSpace(reason) ? JobCancellationReasons.CancellationRequested : reason
        }, cancellationToken: cancellationToken);
    }

    /// <summary>Requests another attempt for a faulted or canceled job.</summary>
    /// <param name="publishEndpoint">The endpoint used to publish the retry command.</param>
    /// <param name="jobId">The identifier of the job to retry.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the command has been published.</returns>
    public static Task RetryJobAsync(
        this IPublishEndpoint publishEndpoint,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ValidateJobId(jobId);

        return publishEndpoint.PublishAsync<IRetryJob>(
            new RetryJobCommand { JobId = jobId },
            cancellationToken: cancellationToken);
    }

    /// <summary>Removes a terminal job and its retained attempts from persistence.</summary>
    /// <param name="publishEndpoint">The endpoint used to publish the finalization command.</param>
    /// <param name="jobId">The identifier of the job to finalize.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>A task that completes when the command has been published.</returns>
    public static Task FinalizeJobAsync(
        this IPublishEndpoint publishEndpoint,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ValidateJobId(jobId);

        return publishEndpoint.PublishAsync<IFinalizeJob>(
            new FinalizeJobCommand { JobId = jobId },
            cancellationToken: cancellationToken);
    }

    static SubmitJobCommand<TJob> CreateSubmitJobCommand<TJob>(
        Guid jobId,
        TJob job,
        Action<ISetPropertyCollection>? setJobProperties)
        where TJob : class
    {
        var command = new SubmitJobCommand<TJob>
        {
            JobId = jobId,
            Job = job
        };

        if (setJobProperties is not null)
        {
            var properties = new JobPropertyCollection();
            setJobProperties(properties);

            if (properties.Count > 0)
                command.JobProperties = properties.Values;
        }

        return command;
    }

    static void ValidateJobId(Guid jobId)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("The job identifier cannot be empty.", nameof(jobId));
    }
}
