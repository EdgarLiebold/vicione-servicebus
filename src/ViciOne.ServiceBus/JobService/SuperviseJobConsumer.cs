using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Consumes supervise job messages.</summary>
public class SuperviseJobConsumer :
    IConsumer<CancelJobAttempt>,
    IConsumer<GetJobAttemptStatus>
{
    readonly IJobService _jobService;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="jobService">The job service.</param>
    public SuperviseJobConsumer(IJobService jobService)
    {
        _jobService = jobService;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ConsumeAsync(ConsumeContext<CancelJobAttempt> context)
    {
        if (_jobService.TryGetJob(context.Message.JobId, out var handle))
        {
            await handle.CancelAsync(context.Message.GetCancellationReason()).ConfigureAwait(false);
        }
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<GetJobAttemptStatus> context)
    {
        if (_jobService.TryGetJob(context.Message.JobId, out var jobHandle))
        {
            return context.RespondAsync<JobAttemptStatus>(new JobAttemptStatusResponse
            {
                JobId = context.Message.JobId,
                AttemptId = context.Message.AttemptId,
                Timestamp = context.GetUtcDateTime(),
                Status = jobHandle.JobTask.Status switch
                {
                    TaskStatus.RanToCompletion => JobStatus.Completed,
                    TaskStatus.Faulted => JobStatus.Faulted,
                    TaskStatus.Canceled => JobStatus.Canceled,
                    _ => JobStatus.Running
                }
            });
        }

        LogContext.Debug?.Log("CheckJobStatus, job not found: {JobId}", context.Message.JobId);

        return Task.CompletedTask;
    }
}
