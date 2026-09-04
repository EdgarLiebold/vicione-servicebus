using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Serialization;

#nullable enable
namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Provides extension methods for job service.
/// </summary>
public static class JobServiceExtensions
{
    /// <summary>
    /// Requests the job state for the specified <paramref name="jobId" /> using the request client
    /// </summary>
    /// <param name="client"></param>
    /// <param name="jobId"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task<JobState> GetJobStateAsync(this IRequestClient<GetJobState> client, Guid jobId, CancellationToken cancellationToken = default)
    {
        Response<JobState> response = await client.GetResponseAsync<JobState>(new GetJobStateRequest { JobId = jobId }, cancellationToken: cancellationToken);

        return response.Message;
    }

    /// <summary>
    /// Requests the job state for the specified <paramref name="jobId" /> using the request client
    /// </summary>
    /// <param name="client"></param>
    /// <param name="jobId"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static async Task<JobState<T>> GetJobStateAsync<T>(this IRequestClient<GetJobState> client, Guid jobId, CancellationToken cancellationToken = default)
        where T : class
    {
        Response<JobState> response = await client.GetResponseAsync<JobState>(new GetJobStateRequest { JobId = jobId }, cancellationToken: cancellationToken);

        if (response is MessageResponse<JobState> messageResponse)
            return new JobStateResponse<T>(response.Message, messageResponse.DeserializeObject<T>(response.Message.JobState!));

        return new JobStateResponse<T>(response.Message);
    }

    /// <summary>
    /// Submits a job, returning the generated jobId
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Task<Guid> SubmitJobAsync<T>(this IPublishEndpoint publishEndpoint, T job, CancellationToken cancellationToken = default)
        where T : class
    {
        return SubmitJobAsync(publishEndpoint, NewId.NextGuid(), job, null, cancellationToken);
    }

    /// <summary>
    /// Submits a job, returning the generated jobId
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="job"></param>
    /// <param name="setJobProperties"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Task<Guid> SubmitJobAsync<T>(this IPublishEndpoint publishEndpoint, T job, Action<ISetPropertyCollection>? setJobProperties = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return SubmitJobAsync(publishEndpoint, NewId.NextGuid(), job, setJobProperties, cancellationToken);
    }

    /// <summary>
    /// Submits a job, returning the generated jobId
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="jobId">A unique job id</param>
    /// <param name="job"></param>
    /// <param name="setJobProperties"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> SubmitJobAsync<T>(this IPublishEndpoint publishEndpoint, Guid jobId, T job, Action<ISetPropertyCollection>?
        setJobProperties = null, CancellationToken cancellationToken = default)
        where T : class
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("An empty Guid cannot be used as a JobId", nameof(jobId));

        await publishEndpoint.PublishAsync<SubmitJob<T>>(CreateSubmitJobCommand(jobId, job, setJobProperties), cancellationToken).ConfigureAwait(false);

        return jobId;
    }

    static SubmitJobCommand<T> CreateSubmitJobCommand<T>(Guid jobId, T job, Action<ISetPropertyCollection>? setJobProperties)
        where T : class
    {
        var command = new SubmitJobCommand<T>
        {
            JobId = jobId,
            Job = job
        };

        if (setJobProperties != null)
        {
            var properties = new JobPropertyCollection();
            setJobProperties(properties);

            if (properties.Count > 0)
                command.Properties = properties;
        }

        return command;
    }

    /// <summary>
    /// Submits a job, returning the generated jobId
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Task<Guid> SubmitJobAsync<T>(this IPublishEndpoint publishEndpoint, object job, CancellationToken cancellationToken = default)
        where T : class
    {
        return SubmitJobAsync<T>(publishEndpoint, NewId.NextGuid(), job, null, cancellationToken);
    }

    /// <summary>
    /// Submits a job, returning the generated jobId
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="job"></param>
    /// <param name="setJobProperties"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Task<Guid> SubmitJobAsync<T>(this IPublishEndpoint publishEndpoint, object job, Action<ISetPropertyCollection>? setJobProperties = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return SubmitJobAsync<T>(publishEndpoint, NewId.NextGuid(), job, setJobProperties, cancellationToken);
    }

    /// <summary>
    /// Submits a job, returning the generated jobId
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="jobId">A unique job id</param>
    /// <param name="job"></param>
    /// <param name="setJobProperties"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> SubmitJobAsync<T>(this IPublishEndpoint publishEndpoint, Guid jobId, object job, Action<ISetPropertyCollection>?
        setJobProperties = null, CancellationToken cancellationToken = default)
        where T : class
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("An empty Guid cannot be used as a JobId", nameof(jobId));

        InitializeContext<T> context = await MessageInitializerCache<T>.InitializeAsync(job, cancellationToken).ConfigureAwait(false);

        await publishEndpoint.PublishAsync<SubmitJob<T>>(CreateSubmitJobCommand(jobId, context.Message, setJobProperties), cancellationToken)
            .ConfigureAwait(false);

        return jobId;
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Task<Guid> SubmitJobAsync<T>(this IRequestClient<SubmitJob<T>> client, T job, CancellationToken cancellationToken = default)
        where T : class
    {
        return SubmitJobAsync(client, NewId.NextGuid(), job, null, cancellationToken);
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="job"></param>
    /// <param name="setJobProperties"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Task<Guid> SubmitJobAsync<T>(this IRequestClient<SubmitJob<T>> client, T job, Action<ISetPropertyCollection>? setJobProperties = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return SubmitJobAsync(client, NewId.NextGuid(), job, setJobProperties, cancellationToken);
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="jobId">A unique job id</param>
    /// <param name="job"></param>
    /// <param name="setJobProperties"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> SubmitJobAsync<T>(this IRequestClient<SubmitJob<T>> client, Guid jobId, T job,
        Action<ISetPropertyCollection>? setJobProperties = null, CancellationToken cancellationToken = default)
        where T : class
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("An empty Guid cannot be used as a JobId", nameof(jobId));

        Response<JobSubmissionAccepted> response = await client.GetResponseAsync<JobSubmissionAccepted>(CreateSubmitJobCommand(jobId, job, setJobProperties),
            cancellationToken).ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Task<Guid> SubmitJobAsync<T>(this IRequestClient<SubmitJob<T>> client, object job, CancellationToken cancellationToken = default)
        where T : class
    {
        return SubmitJobAsync(client, NewId.NextGuid(), job, null, cancellationToken);
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="job"></param>
    /// <param name="setJobProperties"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Task<Guid> SubmitJobAsync<T>(this IRequestClient<SubmitJob<T>> client, object job, Action<ISetPropertyCollection>? setJobProperties = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        return SubmitJobAsync(client, NewId.NextGuid(), job, setJobProperties, cancellationToken);
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="jobId">Specify an explicit jobId for the job</param>
    /// <param name="job"></param>
    /// <param name="setJobProperties"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> SubmitJobAsync<T>(this IRequestClient<SubmitJob<T>> client, Guid jobId, object job,
        Action<ISetPropertyCollection>? setJobProperties = null, CancellationToken cancellationToken = default)
        where T : class
    {
        InitializeContext<T> context = await MessageInitializerCache<T>.InitializeAsync(job, cancellationToken).ConfigureAwait(false);

        Response<JobSubmissionAccepted> response = await client.GetResponseAsync<JobSubmissionAccepted>(CreateSubmitJobCommand(jobId, context.Message,
            setJobProperties), cancellationToken).ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> SubmitJobAsync<T>(this IRequestClient<T> client, T job, CancellationToken cancellationToken = default)
        where T : class
    {
        Response<JobSubmissionAccepted> response = await client.GetResponseAsync<JobSubmissionAccepted>(job, cancellationToken).ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> SubmitJobAsync<T>(this IRequestClient<T> client, object job, CancellationToken cancellationToken = default)
        where T : class
    {
        Response<JobSubmissionAccepted> response = await client.Advanced()
            .GetResponseAsync<JobSubmissionAccepted>(job, timeout: default, cancellationToken)
            .ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>
    /// Cancel a job if the job exists and is in a state that can be canceled.
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="jobId"></param>
    /// <param name="reason"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task CancelJobAsync(this IPublishEndpoint publishEndpoint, Guid jobId, string? reason = null, CancellationToken cancellationToken = default)
    {
        return publishEndpoint.PublishAsync<CancelJob>(new CancelJobCommand
        {
            JobId = jobId,
            Reason = reason ?? "Unspecified"
        }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retry a job if the job exists and is in a state that can be retried.
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="jobId"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task RetryJobAsync(this IPublishEndpoint publishEndpoint, Guid jobId, CancellationToken cancellationToken = default)
    {
        return publishEndpoint.PublishAsync<RetryJob>(new RetryJobCommand { JobId = jobId }, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Finalize a job, removing any faulted job attempts, so that it can be removed from the saga repository
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="jobId"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task FinalizeJobAsync(this IPublishEndpoint publishEndpoint, Guid jobId, CancellationToken cancellationToken = default)
    {
        return publishEndpoint.PublishAsync<FinalizeJob>(new FinalizeJobCommand { JobId = jobId }, cancellationToken: cancellationToken);
    }
}
