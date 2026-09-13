using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Provides durable one-time and recurring job scheduling operations.</summary>
public static class RecurringJobExtensions
{
    /// <summary>Creates or replaces a named recurring job using a cron expression.</summary>
    /// <typeparam name="TJob">The recurring job contract type.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <param name="job">The job submitted on each occurrence.</param>
    /// <param name="cronExpression">The cron expression that defines occurrence times.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The deterministic identifier of the accepted recurring job.</returns>
    public static async Task<Guid> AddOrUpdateRecurringJobAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        string jobName,
        TJob job,
        string cronExpression,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobName(jobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);

        Guid jobId = RecurringJobIdentity<TJob>.CreateId(jobName);
        JobScheduleInfo schedule = CreateRecurringSchedule(cronExpression);

        Response<IJobSubmissionAccepted> response = await client
            .GetResponseAsync<IJobSubmissionAccepted>(CreateCommand(jobId, job, schedule, null), cancellationToken)
            .ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>Creates or replaces a named recurring job using a schedule callback.</summary>
    /// <typeparam name="TJob">The recurring job contract type.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <param name="job">The job submitted on each occurrence.</param>
    /// <param name="configure">The callback that defines the recurring schedule.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The deterministic identifier of the accepted recurring job.</returns>
    public static Task<Guid> AddOrUpdateRecurringJobAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        string jobName,
        TJob job,
        Action<IRecurringJobScheduleConfigurator> configure,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        return AddOrUpdateRecurringJobAsync(client, jobName, job, configure, null, cancellationToken);
    }

    /// <summary>Creates or replaces a named recurring job with schedule and job-property callbacks.</summary>
    /// <typeparam name="TJob">The recurring job contract type.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <param name="job">The job submitted on each occurrence.</param>
    /// <param name="configure">The callback that defines the recurring schedule.</param>
    /// <param name="setJobProperties">The optional callback that adds application-defined job properties.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The deterministic identifier of the accepted recurring job.</returns>
    public static async Task<Guid> AddOrUpdateRecurringJobAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        string jobName,
        TJob job,
        Action<IRecurringJobScheduleConfigurator> configure,
        Action<ISetPropertyCollection>? setJobProperties,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(configure);
        ValidateJobName(jobName);

        Guid jobId = RecurringJobIdentity<TJob>.CreateId(jobName);
        JobScheduleInfo schedule = CreateRecurringSchedule(configure);

        Response<IJobSubmissionAccepted> response = await client
            .GetResponseAsync<IJobSubmissionAccepted>(CreateCommand(jobId, job, schedule, setJobProperties), cancellationToken)
            .ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>Publishes a request to create or replace a named recurring job using a cron expression.</summary>
    /// <typeparam name="TJob">The recurring job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <param name="job">The job submitted on each occurrence.</param>
    /// <param name="cronExpression">The cron expression that defines occurrence times.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The deterministic identifier assigned to the recurring job.</returns>
    public static async Task<Guid> AddOrUpdateRecurringJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        string jobName,
        TJob job,
        string cronExpression,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobName(jobName);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);

        Guid jobId = RecurringJobIdentity<TJob>.CreateId(jobName);
        JobScheduleInfo schedule = CreateRecurringSchedule(cronExpression);

        await publishEndpoint
            .PublishAsync<ISubmitJob<TJob>>(CreateCommand(jobId, job, schedule, null), cancellationToken)
            .ConfigureAwait(false);

        return jobId;
    }

    /// <summary>Publishes a request to create or replace a named recurring job using a schedule callback.</summary>
    /// <typeparam name="TJob">The recurring job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <param name="job">The job submitted on each occurrence.</param>
    /// <param name="configure">The callback that defines the recurring schedule.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The deterministic identifier assigned to the recurring job.</returns>
    public static Task<Guid> AddOrUpdateRecurringJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        string jobName,
        TJob job,
        Action<IRecurringJobScheduleConfigurator> configure,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        return AddOrUpdateRecurringJobAsync(publishEndpoint, jobName, job, configure, null, cancellationToken);
    }

    /// <summary>Publishes a request to create or replace a named recurring job with schedule and job-property callbacks.</summary>
    /// <typeparam name="TJob">The recurring job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <param name="job">The job submitted on each occurrence.</param>
    /// <param name="configure">The callback that defines the recurring schedule.</param>
    /// <param name="setJobProperties">The optional callback that adds application-defined job properties.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The deterministic identifier assigned to the recurring job.</returns>
    public static async Task<Guid> AddOrUpdateRecurringJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        string jobName,
        TJob job,
        Action<IRecurringJobScheduleConfigurator> configure,
        Action<ISetPropertyCollection>? setJobProperties,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(configure);
        ValidateJobName(jobName);

        Guid jobId = RecurringJobIdentity<TJob>.CreateId(jobName);
        JobScheduleInfo schedule = CreateRecurringSchedule(configure);

        await publishEndpoint
            .PublishAsync<ISubmitJob<TJob>>(CreateCommand(jobId, job, schedule, setJobProperties), cancellationToken)
            .ConfigureAwait(false);

        return jobId;
    }

    /// <summary>Requests cancellation of a named recurring job.</summary>
    /// <typeparam name="TJob">The recurring job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the cancellation command.</param>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <param name="reason">The optional application reason recorded with the cancellation.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The deterministic identifier of the recurring job.</returns>
    public static async Task<Guid> CancelRecurringJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        string jobName,
        string? reason = null,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ValidateJobName(jobName);

        Guid jobId = RecurringJobIdentity<TJob>.CreateId(jobName);

        await publishEndpoint.PublishAsync<ICancelJob>(new CancelJobCommand
        {
            JobId = jobId,
            Reason = string.IsNullOrWhiteSpace(reason) ? JobCancellationReasons.CancellationRequested : reason
        }, cancellationToken).ConfigureAwait(false);

        return jobId;
    }

    /// <summary>Removes a terminal named recurring job and its retained attempts from persistence.</summary>
    /// <typeparam name="TJob">The recurring job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the finalization command.</param>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The deterministic identifier of the recurring job.</returns>
    public static async Task<Guid> FinalizeRecurringJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        string jobName,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ValidateJobName(jobName);

        Guid jobId = RecurringJobIdentity<TJob>.CreateId(jobName);

        await publishEndpoint
            .PublishAsync<IFinalizeJob>(new FinalizeJobCommand { JobId = jobId }, cancellationToken)
            .ConfigureAwait(false);

        return jobId;
    }

    /// <summary>Publishes a job that becomes eligible at a specified instant.</summary>
    /// <typeparam name="TJob">The scheduled job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="start">The earliest instant at which the job may run.</param>
    /// <param name="job">The job to schedule.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The generated job identifier.</returns>
    public static Task<Guid> ScheduleJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        DateTimeOffset start,
        TJob job,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        return ScheduleJobAsync(publishEndpoint, NewId.NextGuid(), start, job, cancellationToken);
    }

    /// <summary>Publishes a job with an explicit identifier that becomes eligible at a specified instant.</summary>
    /// <typeparam name="TJob">The scheduled job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="jobId">The identifier assigned to the job.</param>
    /// <param name="start">The earliest instant at which the job may run.</param>
    /// <param name="job">The job to schedule.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The supplied job identifier.</returns>
    public static async Task<Guid> ScheduleJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        Guid jobId,
        DateTimeOffset start,
        TJob job,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobId(jobId);

        await publishEndpoint
            .PublishAsync<ISubmitJob<TJob>>(CreateScheduledCommand(jobId, start, job), cancellationToken)
            .ConfigureAwait(false);

        return jobId;
    }

    /// <summary>Initializes and publishes a scheduled job from property values.</summary>
    /// <typeparam name="TJob">The scheduled job contract type to initialize.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="start">The earliest instant at which the job may run.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="cancellationToken">The token that cancels initialization or publication.</param>
    /// <returns>The generated job identifier.</returns>
    public static Task<Guid> ScheduleJobFromValuesAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        DateTimeOffset start,
        object values,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        return ScheduleJobFromValuesAsync<TJob>(publishEndpoint, NewId.NextGuid(), start, values, cancellationToken);
    }

    /// <summary>Initializes and publishes a scheduled job from property values with an explicit identifier.</summary>
    /// <typeparam name="TJob">The scheduled job contract type to initialize.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the submission.</param>
    /// <param name="jobId">The identifier assigned to the job.</param>
    /// <param name="start">The earliest instant at which the job may run.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="cancellationToken">The token that cancels initialization or publication.</param>
    /// <returns>The supplied job identifier.</returns>
    public static async Task<Guid> ScheduleJobFromValuesAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        Guid jobId,
        DateTimeOffset start,
        object values,
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
            .PublishAsync<ISubmitJob<TJob>>(CreateScheduledCommand(jobId, start, context.Message), cancellationToken)
            .ConfigureAwait(false);

        return jobId;
    }

    /// <summary>Submits a job request that becomes eligible at a specified instant.</summary>
    /// <typeparam name="TJob">The scheduled job contract type.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="start">The earliest instant at which the job may run.</param>
    /// <param name="job">The job to schedule.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static Task<Guid> ScheduleJobAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        DateTimeOffset start,
        TJob job,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        return ScheduleJobAsync(client, NewId.NextGuid(), start, job, cancellationToken);
    }

    /// <summary>Submits a job request with an explicit identifier that becomes eligible at a specified instant.</summary>
    /// <typeparam name="TJob">The scheduled job contract type.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="jobId">The identifier assigned to the job.</param>
    /// <param name="start">The earliest instant at which the job may run.</param>
    /// <param name="job">The job to schedule.</param>
    /// <param name="cancellationToken">The token that cancels the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static async Task<Guid> ScheduleJobAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        Guid jobId,
        DateTimeOffset start,
        TJob job,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(job);
        ValidateJobId(jobId);

        Response<IJobSubmissionAccepted> response = await client
            .GetResponseAsync<IJobSubmissionAccepted>(CreateScheduledCommand(jobId, start, job), cancellationToken)
            .ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>Initializes and submits a scheduled job request from property values.</summary>
    /// <typeparam name="TJob">The scheduled job contract type to initialize.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="start">The earliest instant at which the job may run.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static Task<Guid> ScheduleJobFromValuesAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        DateTimeOffset start,
        object values,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        return ScheduleJobFromValuesAsync<TJob>(client, NewId.NextGuid(), start, values, cancellationToken);
    }

    /// <summary>Initializes and submits a scheduled job request from property values with an explicit identifier.</summary>
    /// <typeparam name="TJob">The scheduled job contract type to initialize.</typeparam>
    /// <param name="client">The request client connected to the job submission endpoint.</param>
    /// <param name="jobId">The identifier assigned to the job.</param>
    /// <param name="start">The earliest instant at which the job may run.</param>
    /// <param name="values">The object whose public values initialize the job contract.</param>
    /// <param name="cancellationToken">The token that cancels initialization or the request.</param>
    /// <returns>The job identifier accepted by the coordinator.</returns>
    public static async Task<Guid> ScheduleJobFromValuesAsync<TJob>(
        this IRequestClient<ISubmitJob<TJob>> client,
        Guid jobId,
        DateTimeOffset start,
        object values,
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
            .GetResponseAsync<IJobSubmissionAccepted>(CreateScheduledCommand(jobId, start, context.Message), cancellationToken)
            .ConfigureAwait(false);

        return response.Message.JobId;
    }

    /// <summary>Requests immediate execution of a named recurring job that is currently waiting.</summary>
    /// <typeparam name="TJob">The recurring job contract type.</typeparam>
    /// <param name="publishEndpoint">The endpoint used to publish the run command.</param>
    /// <param name="jobName">The stable application name of the recurring job.</param>
    /// <param name="cancellationToken">The token that cancels publication.</param>
    /// <returns>The deterministic identifier of the recurring job.</returns>
    public static async Task<Guid> RunRecurringJobAsync<TJob>(
        this IPublishEndpoint publishEndpoint,
        string jobName,
        CancellationToken cancellationToken = default)
        where TJob : class
    {
        ArgumentNullException.ThrowIfNull(publishEndpoint);
        ValidateJobName(jobName);

        Guid jobId = RecurringJobIdentity<TJob>.CreateId(jobName);
        await publishEndpoint
            .PublishAsync<IRunJob>(new RunJobCommand { JobId = jobId }, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return jobId;
    }

    static SubmitJobCommand<TJob> CreateCommand<TJob>(
        Guid jobId,
        TJob job,
        JobScheduleInfo schedule,
        Action<ISetPropertyCollection>? setJobProperties)
        where TJob : class
    {
        var command = new SubmitJobCommand<TJob>
        {
            JobId = jobId,
            Job = job,
            Schedule = schedule
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

    static JobScheduleInfo CreateRecurringSchedule(string cronExpression)
    {
        var schedule = new JobScheduleInfo { CronExpression = cronExpression };
        ValidateRecurringSchedule(schedule);
        return schedule;
    }

    static JobScheduleInfo CreateRecurringSchedule(Action<IRecurringJobScheduleConfigurator> configure)
    {
        var schedule = new JobScheduleInfo();
        configure(schedule);
        ValidateRecurringSchedule(schedule);
        return schedule;
    }

    static SubmitJobCommand<TJob> CreateScheduledCommand<TJob>(Guid jobId, DateTimeOffset start, TJob job)
        where TJob : class
    {
        return new SubmitJobCommand<TJob>
        {
            JobId = jobId,
            Job = job,
            Schedule = new JobScheduleInfo { Start = start.ToUniversalTime() }
        };
    }

    static void ValidateRecurringSchedule(JobScheduleInfo schedule)
    {
        if (string.IsNullOrWhiteSpace(schedule.CronExpression))
        {
            throw new ConfigurationException(
                global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Recurring job schedule",
                    "CronExpression",
                    "A recurring schedule must define a cron expression.",
                    "Configure the recurring job with a cron expression or one of the recurring schedule helpers."));
        }

        schedule.Validate().ThrowIfContainsFailure("The recurring schedule configuration is invalid:");
    }

    static void ValidateJobId(Guid jobId)
    {
        if (jobId == Guid.Empty)
            throw new ArgumentException("The job identifier cannot be empty.", nameof(jobId));
    }

    static void ValidateJobName(string jobName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
    }
}
