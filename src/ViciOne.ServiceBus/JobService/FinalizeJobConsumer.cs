using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Consumes finalize job messages.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
public class FinalizeJobConsumer<TJob> :
    IConsumer<FaultJob>,
    IConsumer<CompleteJob>
    where TJob : class
{
    readonly Guid _jobTypeId;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="jobTypeId">The job type id.</param>
    public FinalizeJobConsumer(Guid jobTypeId)
    {
        _jobTypeId = jobTypeId;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<CompleteJob> context)
    {
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
        });
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<FaultJob> context)
    {
        var message = context.Message;
        if (message.JobTypeId != _jobTypeId)
            return Task.CompletedTask;

        var job = context.GetJob<TJob>() ?? throw new SerializationException($"The job could not be deserialized: {TypeCache<TJob>.ShortName}");

        var jobContext = new FaultJobContext<TJob>(context, job);

        return jobContext.GenerateFaultAsync(new ExceptionInfoException(message.Exceptions));
    }
}
