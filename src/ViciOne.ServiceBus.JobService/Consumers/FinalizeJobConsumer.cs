using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Publishes strongly typed completion and fault notifications for a terminal job.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
internal sealed class FinalizeJobConsumer<TJob> :
    IConsumer<FaultJob>,
    IConsumer<CompleteJob>
    where TJob : class
{
    readonly Guid _jobTypeId;

    /// <summary>Initializes a finalizer for a registered job type.</summary>
    /// <param name="jobTypeId">The stable identifier of the registered job type.</param>
    public FinalizeJobConsumer(Guid jobTypeId)
    {
        if (jobTypeId == Guid.Empty)
            throw new ArgumentException("The job type identifier cannot be empty.", nameof(jobTypeId));

        _jobTypeId = jobTypeId;
    }

    /// <summary>Publishes the strongly typed completion event when the command belongs to this job type.</summary>
    /// <param name="context">The completed job and its serialized payload.</param>
    /// <returns>The completion-publication task.</returns>
    public Task ConsumeAsync(ConsumeContext<CompleteJob> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Message.JobTypeId != _jobTypeId)
            return Task.CompletedTask;

        var job = context.GetJob<TJob>() ?? throw new SerializationException($"The job could not be deserialized: {TypeCache<TJob>.ShortName}");

        return context.Advanced().PublishAsync<JobCompleted<TJob>>(new JobCompletedEvent<TJob>
        {
            JobId = context.Message.JobId,
            Timestamp = context.Message.Timestamp,
            Duration = context.Message.Duration,
            Job = job,
            JobProperties = context.Message.JobProperties,
            InstanceProperties = context.Message.InstanceProperties,
            JobTypeProperties = context.Message.JobTypeProperties,
        }, context.CancellationToken);
    }

    /// <summary>Publishes the strongly typed fault event when the command belongs to this job type.</summary>
    /// <param name="context">The faulted job, its serialized payload, and captured exceptions.</param>
    /// <returns>The fault-publication task.</returns>
    public Task ConsumeAsync(ConsumeContext<FaultJob> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var message = context.Message;
        if (message.JobTypeId != _jobTypeId)
            return Task.CompletedTask;

        var job = context.GetJob<TJob>() ?? throw new SerializationException($"The job could not be deserialized: {TypeCache<TJob>.ShortName}");

        var jobContext = new FaultJobContext<TJob>(context, job);

        return jobContext.GenerateFaultAsync(new ExceptionInfoException(message.Exceptions));
    }
}
