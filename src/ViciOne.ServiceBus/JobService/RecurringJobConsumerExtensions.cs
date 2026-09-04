using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus;

public static class RecurringJobConsumerExtensions
{
    /// <summary>
    /// Add or update a recurring job
    /// </summary>
    /// <param name="client">An existing request client</param>
    /// <param name="jobName"></param>
    /// <param name="job"></param>
    /// <param name="cronExpression">The scheduler cron expression</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> AddOrUpdateRecurringJobAsync<T>(this IRequestClient<SubmitJob<T>> client, string jobName, T job, string cronExpression,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(jobName))
            throw new ArgumentNullException(nameof(jobName));
        if (cronExpression == null)
            throw new ArgumentNullException(nameof(cronExpression));

        var jobId = JobMetadataCache<T>.GenerateRecurringJobId(jobName);

        var schedule = new RecurringJobScheduleInfo { CronExpression = cronExpression };
        schedule.Validate().ThrowIfContainsFailure("The schedule configuration is invalid:");

        Response<JobSubmissionAccepted> response = await client.GetResponseAsync<JobSubmissionAccepted>(new SubmitJobCommand<T>
        {
            JobId = jobId,
            Job = job,
            Schedule = schedule
        }, cancellationToken).ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>
    /// Add or update a recurring job
    /// </summary>
    /// <param name="client">An existing request client</param>
    /// <param name="jobName"></param>
    /// <param name="job"></param>
    /// <param name="configure">Configure the optional recurring job schedule parameters</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Task<Guid> AddOrUpdateRecurringJobAsync<T>(this IRequestClient<SubmitJob<T>> client, string jobName, T job,
        Action<IRecurringJobScheduleConfigurator> configure, CancellationToken cancellationToken = default)
        where T : class
    {
        return AddOrUpdateRecurringJobAsync(client, jobName, job, configure, null, cancellationToken);
    }

    /// <summary>
    /// Add or update a recurring job
    /// </summary>
    /// <param name="client">An existing request client</param>
    /// <param name="jobName"></param>
    /// <param name="job"></param>
    /// <param name="configure">Configure the optional recurring job schedule parameters</param>
    /// <param name="setJobProperties">Set job properties for the recurring job</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> AddOrUpdateRecurringJobAsync<T>(this IRequestClient<SubmitJob<T>> client, string jobName, T job,
        Action<IRecurringJobScheduleConfigurator> configure, Action<ISetPropertyCollection>? setJobProperties, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(jobName))
            throw new ArgumentNullException(nameof(jobName));

        var jobId = JobMetadataCache<T>.GenerateRecurringJobId(jobName);

        var schedule = new RecurringJobScheduleInfo();
        configure?.Invoke(schedule);

        schedule.Validate().ThrowIfContainsFailure("The schedule configuration is invalid:");

        var command = new SubmitJobCommand<T>
        {
            JobId = jobId,
            Job = job,
            Schedule = schedule
        };

        if (setJobProperties != null)
        {
            var properties = new JobPropertyCollection();
            setJobProperties(properties);

            if (properties.Count > 0)
                command.Properties = properties;
        }

        Response<JobSubmissionAccepted> response = await client.GetResponseAsync<JobSubmissionAccepted>(command, cancellationToken).ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>
    /// Add or update a recurring job
    /// </summary>
    /// <param name="publishEndpoint">An available publish endpoint instance</param>
    /// <param name="jobName"></param>
    /// <param name="job"></param>
    /// <param name="cronExpression">The scheduler cron expression</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> AddOrUpdateRecurringJobAsync<T>(this IPublishEndpoint publishEndpoint, string jobName, T job, string cronExpression,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(jobName))
            throw new ArgumentNullException(nameof(jobName));
        if (cronExpression == null)
            throw new ArgumentNullException(nameof(cronExpression));

        var jobId = JobMetadataCache<T>.GenerateRecurringJobId(jobName);

        var schedule = new RecurringJobScheduleInfo { CronExpression = cronExpression };
        schedule.Validate().ThrowIfContainsFailure("The schedule configuration is invalid:");

        await publishEndpoint.PublishAsync<SubmitJob<T>>(new SubmitJobCommand<T>
        {
            JobId = jobId,
            Job = job,
            Schedule = schedule
        }, cancellationToken).ConfigureAwait(false);

        return jobId;
    }

    /// <summary>
    /// Add or update a recurring job
    /// </summary>
    /// <param name="publishEndpoint">An available publish endpoint instance</param>
    /// <param name="jobName"></param>
    /// <param name="job"></param>
    /// <param name="configure">Configure the optional recurring job schedule parameters</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static Task<Guid> AddOrUpdateRecurringJobAsync<T>(this IPublishEndpoint publishEndpoint, string jobName, T job,
        Action<IRecurringJobScheduleConfigurator> configure, CancellationToken cancellationToken = default)
        where T : class
    {
        return AddOrUpdateRecurringJobAsync(publishEndpoint, jobName, job, configure, null, cancellationToken);
    }

    /// <summary>
    /// Add or update a recurring job
    /// </summary>
    /// <param name="publishEndpoint">An available publish endpoint instance</param>
    /// <param name="jobName"></param>
    /// <param name="job"></param>
    /// <param name="configure">Configure the optional recurring job schedule parameters</param>
    /// <param name="setJobProperties">Set job properties for the recurring job</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> AddOrUpdateRecurringJobAsync<T>(this IPublishEndpoint publishEndpoint, string jobName, T job,
        Action<IRecurringJobScheduleConfigurator> configure, Action<ISetPropertyCollection>? setJobProperties, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(jobName))
            throw new ArgumentNullException(nameof(jobName));

        var jobId = JobMetadataCache<T>.GenerateRecurringJobId(jobName);

        var schedule = new RecurringJobScheduleInfo();
        configure?.Invoke(schedule);

        schedule.Validate().ThrowIfContainsFailure("The schedule configuration is invalid:");

        var command = new SubmitJobCommand<T>
        {
            JobId = jobId,
            Job = job,
            Schedule = schedule
        };

        if (setJobProperties != null)
        {
            var properties = new JobPropertyCollection();
            setJobProperties(properties);

            if (properties.Count > 0)
                command.Properties = properties;
        }

        await publishEndpoint.PublishAsync<SubmitJob<T>>(command, cancellationToken).ConfigureAwait(false);

        return jobId;
    }

    /// <summary>
    /// Cancel a recurring job
    /// </summary>
    /// <param name="publishEndpoint">An available publish endpoint instance</param>
    /// <param name="jobName"></param>
    /// <param name="reason">The reason for canceling the job</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> CancelRecurringJobAsync<T>(this IPublishEndpoint publishEndpoint, string jobName, string reason,
        CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(jobName))
            throw new ArgumentNullException(nameof(jobName));

        var jobId = JobMetadataCache<T>.GenerateRecurringJobId(jobName);

        await publishEndpoint.PublishAsync<CancelJob>(new CancelJobCommand
        {
            JobId = jobId,
            Reason = reason ?? "Unspecified"
        }, cancellationToken).ConfigureAwait(false);

        return jobId;
    }

    /// <summary>
    /// Finalize a canceled recurring job
    /// </summary>
    /// <param name="publishEndpoint">An available publish endpoint instance</param>
    /// <param name="jobName"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> FinalizeRecurringJobAsync<T>(this IPublishEndpoint publishEndpoint, string jobName, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(jobName))
            throw new ArgumentNullException(nameof(jobName));

        var jobId = JobMetadataCache<T>.GenerateRecurringJobId(jobName);

        await publishEndpoint.PublishAsync<FinalizeJob>(new FinalizeJobCommand { JobId = jobId }, cancellationToken).ConfigureAwait(false);

        return jobId;
    }

    /// <summary>
    /// Submits a job, returning the generated jobId
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="start">The start time for the job</param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> ScheduleJobAsync<T>(this IPublishEndpoint publishEndpoint, DateTimeOffset start, T job,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var jobId = NewId.NextGuid();

        await publishEndpoint.PublishAsync<SubmitJob<T>>(new SubmitJobCommand<T>
        {
            JobId = jobId,
            Job = job,
            Schedule = new RecurringJobScheduleInfo { Start = start.ToUniversalTime() }
        }, cancellationToken).ConfigureAwait(false);

        return jobId;
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="start">The start time for the job</param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> ScheduleJobAsync<T>(this IRequestClient<SubmitJob<T>> client, DateTimeOffset start, T job,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var jobId = NewId.NextGuid();

        Response<JobSubmissionAccepted> response = await client.GetResponseAsync<JobSubmissionAccepted>(new SubmitJobCommand<T>
        {
            JobId = jobId,
            Job = job,
            Schedule = new RecurringJobScheduleInfo { Start = start.ToUniversalTime() }
        }, cancellationToken).ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="start">The start time for the job</param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> ScheduleJobAsync<T>(this IRequestClient<SubmitJob<T>> client, DateTimeOffset start, object job,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var jobId = NewId.NextGuid();

        InitializeContext<T> context = await MessageInitializerCache<T>.InitializeAsync(job, cancellationToken).ConfigureAwait(false);

        Response<JobSubmissionAccepted> response = await client.GetResponseAsync<JobSubmissionAccepted>(new SubmitJobCommand<T>
        {
            JobId = jobId,
            Job = context.Message,
            Schedule = new RecurringJobScheduleInfo { Start = start.ToUniversalTime() }
        }, cancellationToken).ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="jobId">Specify an explicit jobId for the job</param>
    /// <param name="start">The start time for the job</param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> ScheduleJobAsync<T>(this IRequestClient<SubmitJob<T>> client, Guid jobId, DateTimeOffset start, T job,
        CancellationToken cancellationToken = default)
        where T : class
    {
        Response<JobSubmissionAccepted> response = await client.GetResponseAsync<JobSubmissionAccepted>(new SubmitJobCommand<T>
        {
            JobId = jobId,
            Job = job,
            Schedule = new RecurringJobScheduleInfo { Start = start.ToUniversalTime() }
        }, cancellationToken).ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>
    /// Submits a job, returning the accepted jobId
    /// </summary>
    /// <param name="client"></param>
    /// <param name="jobId">Specify an explicit jobId for the job</param>
    /// <param name="start">The start time for the job</param>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static async Task<Guid> ScheduleJobAsync<T>(this IRequestClient<SubmitJob<T>> client, Guid jobId, DateTimeOffset start, object job,
        CancellationToken cancellationToken = default)
        where T : class
    {
        InitializeContext<T> context = await MessageInitializerCache<T>.InitializeAsync(job, cancellationToken).ConfigureAwait(false);

        Response<JobSubmissionAccepted> response = await client.GetResponseAsync<JobSubmissionAccepted>(new SubmitJobCommand<T>
        {
            JobId = jobId,
            Job = context.Message,
            Schedule = new RecurringJobScheduleInfo { Start = start.ToUniversalTime() }
        }, cancellationToken).ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>
    /// Run a recurring job if it's currently waiting/scheduled to run
    /// </summary>
    /// <param name="publishEndpoint"></param>
    /// <param name="jobName"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    public static Task RunRecurringJobAsync<T>(this IPublishEndpoint publishEndpoint, string jobName, CancellationToken cancellationToken = default)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(jobName))
            throw new ArgumentNullException(nameof(jobName));

        var jobId = JobMetadataCache<T>.GenerateRecurringJobId(jobName);

        return publishEndpoint.PublishAsync<RunJob>(new RunJobCommand { JobId = jobId }, cancellationToken: cancellationToken);
    }
}
