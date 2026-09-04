using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;
using ViciOne.ServiceBus.JobService.Scheduling;

#nullable enable
namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Handles the <see cref="SubmitJob{TJob}" /> command
/// </summary>
/// <typeparam name="TJob">The job type</typeparam>
public class SubmitJobConsumer<TJob> :
    IConsumer<TJob>,
    IConsumer<SubmitJob<TJob>>
    where TJob : class
{
    readonly Guid _jobTypeId;
    readonly JobOptions<TJob> _options;

    public SubmitJobConsumer(JobOptions<TJob> options, Guid jobTypeId)
    {
        _options = options;
        _jobTypeId = jobTypeId;
    }

    public Task ConsumeAsync(ConsumeContext<SubmitJob<TJob>> context)
    {
        if (context.Message.Schedule != null)
        {
            if (string.IsNullOrWhiteSpace(context.Message.Schedule.CronExpression) && !context.Message.Schedule.Start.HasValue)
                throw new RecurringJobException("A valid cron expression or start date is required");

            if (!string.IsNullOrWhiteSpace(context.Message.Schedule.CronExpression))
                CronExpression.ValidateExpression(context.Message.Schedule.CronExpression!);
        }

        return PublishJobSubmittedAsync(context.Advanced(), context.Message.JobId, context.Message.Job, context.SentTime ?? context.GetUtcDateTime(), context.Message.Schedule,
            context.Message.Properties);
    }

    public Task ConsumeAsync(ConsumeContext<TJob> context)
    {
        var jobId = context.RequestId ?? NewId.NextGuid();

        return PublishJobSubmittedAsync(context.Advanced(), jobId, context.Message, context.SentTime ?? context.GetUtcDateTime(), null, null);
    }

    async Task PublishJobSubmittedAsync(ConsumeContext context, Guid jobId, TJob job, DateTimeOffset timestamp,
        RecurringJobSchedule? schedule,
        Dictionary<string, object>? jobProperties)
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
        });

        if (context.RequestId.HasValue && context.ResponseAddress != null)
            await context.RespondAsync<JobSubmissionAccepted>(new JobSubmissionAcceptedResponse { JobId = jobId });
    }
}
