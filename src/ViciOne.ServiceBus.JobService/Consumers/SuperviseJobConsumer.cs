using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Applies remote cancellation requests and answers liveness checks for local job attempts.</summary>
internal sealed class SuperviseJobConsumer :
    IConsumer<CancelJobAttempt>,
    IConsumer<GetJobAttemptStatus>
{
    readonly IJobService _jobService;

    /// <summary>Initializes a supervisor for the local job runtime.</summary>
    /// <param name="jobService">The runtime that owns active local jobs.</param>
    public SuperviseJobConsumer(IJobService jobService)
    {
        _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
    }

    /// <summary>Cancels the requested local job attempt when it is active.</summary>
    /// <param name="context">The remote cancellation request.</param>
    /// <returns>The cancellation task.</returns>
    public async Task ConsumeAsync(ConsumeContext<CancelJobAttempt> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_jobService.TryGetJob(context.Message.JobId, out var handle)
            && handle.AttemptId == context.Message.AttemptId)
        {
            await handle.CancelAsync(context.Message.GetCancellationReason(), context.CancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Reports the current task state of a local job attempt.</summary>
    /// <param name="context">The status request to answer.</param>
    /// <returns>The response-publication task, or a completed task when the job is not active locally.</returns>
    public Task ConsumeAsync(ConsumeContext<GetJobAttemptStatus> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_jobService.TryGetJob(context.Message.JobId, out var jobHandle)
            && jobHandle.AttemptId == context.Message.AttemptId)
        {
            return context.RespondAsync<JobAttemptStatus>(new JobAttemptStatusResponse
            {
                JobId = context.Message.JobId,
                AttemptId = context.Message.AttemptId,
                Timestamp = context.GetUtcDateTime(),
                Status = jobHandle.Execution.Status switch
                {
                    TaskStatus.RanToCompletion => JobAttemptStatusKind.Completed,
                    TaskStatus.Faulted => JobAttemptStatusKind.Faulted,
                    TaskStatus.Canceled => JobAttemptStatusKind.Canceled,
                    _ => JobAttemptStatusKind.Running
                }
            });
        }

        LogContext.Debug?.Log("CheckJobStatus, job not found: {JobId}", context.Message.JobId);

        return Task.CompletedTask;
    }
}
