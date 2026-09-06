using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.JobService.Scheduling;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Validates job submissions and publishes their durable coordination command.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
internal sealed class SubmitJobConsumer<TJob> :
    IConsumer<TJob>,
    IConsumer<SubmitJob<TJob>>
    where TJob : class
{
    readonly Guid _jobTypeId;
    readonly JobOptions<TJob> _options;

    /// <summary>Initializes a submission consumer for a registered job type.</summary>
    /// <param name="options">The job-type submission and execution options.</param>
    /// <param name="jobTypeId">The stable identifier of the registered job type.</param>
    public SubmitJobConsumer(JobOptions<TJob> options, Guid jobTypeId)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (jobTypeId == Guid.Empty)
            throw new ArgumentException("The job type identifier cannot be empty.", nameof(jobTypeId));

        _jobTypeId = jobTypeId;
    }

    /// <summary>Validates and publishes an explicit job submission.</summary>
    /// <param name="context">The submission, schedule, and request metadata.</param>
    /// <returns>The submission-publication task.</returns>
    public Task ConsumeAsync(ConsumeContext<SubmitJob<TJob>> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Message.JobId == Guid.Empty)
            throw new ArgumentException("The job identifier cannot be empty.", nameof(context));
        ArgumentNullException.ThrowIfNull(context.Message.Job);

        JobScheduleInfo? schedule = null;
        if (context.Message.Schedule != null)
        {
            schedule = new JobScheduleInfo(context.Message.Schedule);
            schedule.Validate().ThrowIfContainsFailure("The job schedule is invalid:");
        }

        return PublishJobSubmittedAsync(context.Advanced(), context.Message.JobId, context.Message.Job, context.SentTime ?? context.GetUtcDateTime(), schedule,
            context.Message.JobProperties, context.CancellationToken);
    }

    /// <summary>Converts a directly consumed job message into a job submission.</summary>
    /// <param name="context">The job message and its transport metadata.</param>
    /// <returns>The submission-publication task.</returns>
    public Task ConsumeAsync(ConsumeContext<TJob> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var jobId = context.RequestId ?? NewId.NextGuid();

        return PublishJobSubmittedAsync(
            context.Advanced(),
            jobId,
            context.Message,
            context.SentTime ?? context.GetUtcDateTime(),
            null,
            null,
            context.CancellationToken);
    }

    async Task PublishJobSubmittedAsync(ConsumeContext context, Guid jobId, TJob job, DateTimeOffset timestamp,
        JobSchedule? schedule,
        IReadOnlyDictionary<string, object>? jobProperties,
        CancellationToken cancellationToken)
    {
        await context.PublishAsync<JobSubmitted>(new JobSubmittedEvent
        {
            JobId = jobId,
            JobTypeId = _jobTypeId,
            Timestamp = timestamp,
            Job = context.ToDictionary(job),
            JobProperties = jobProperties,
            JobTimeout = _options.JobTimeout,
            Schedule = schedule
        }, cancellationToken).ConfigureAwait(false);

        if (context.RequestId.HasValue && context.ResponseAddress != null)
            await context.RespondAsync<JobSubmissionAccepted>(new JobSubmissionAcceptedResponse { JobId = jobId }).ConfigureAwait(false);
    }
}
