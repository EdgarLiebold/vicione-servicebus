using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService.Messages;

namespace ViciOne.ServiceBus.JobService;

/// <summary>
/// Provides a finalize job consumer implementation.
/// </summary>
/// <typeparam name="TJob">The t job type.</typeparam>
public class FinalizeJobConsumer<TJob> :
    IConsumer<FaultJob>,
    IConsumer<CompleteJob>
    where TJob : class
{
    readonly Guid _jobTypeId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="jobTypeId">The job type id value.</param>
    public FinalizeJobConsumer(Guid jobTypeId)
    {
        _jobTypeId = jobTypeId;
    }

    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Consumes the message provided by the context.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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
