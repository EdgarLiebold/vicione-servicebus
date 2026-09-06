using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Consumes start job messages.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
public class StartJobConsumer<TJob> :
    IConsumer<StartJob>
    where TJob : class
{
    readonly IPipe<ConsumeContext<TJob>> _jobPipe;
    readonly IJobService _jobService;
    readonly Guid _jobTypeId;
    readonly JobOptions<TJob> _options;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="jobService">The job service.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="jobTypeId">The job type id.</param>
    /// <param name="jobPipe">The job pipe.</param>
    public StartJobConsumer(IJobService jobService, JobOptions<TJob> options, Guid jobTypeId, IPipe<ConsumeContext<TJob>> jobPipe)
    {
        _jobService = jobService;
        _options = options;
        _jobTypeId = jobTypeId;
        _jobPipe = jobPipe;
    }

    /// <summary>Consumes the message provided by the context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ConsumeAsync(ConsumeContext<StartJob> context)
    {
        if (context.Message.JobTypeId != _jobTypeId)
            return Task.CompletedTask;

        var job = context.GetJob<TJob>() ?? throw new SerializationException($"The job could not be deserialized: {TypeCache<TJob>.ShortName}");

        return _jobService.StartJobAsync(context, job, _jobPipe, _options);
    }
}
