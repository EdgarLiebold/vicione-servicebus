using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

/// <summary>Deserializes an assigned job and starts its local consumer pipeline.</summary>
/// <typeparam name="TJob">The job type.</typeparam>
internal sealed class StartJobConsumer<TJob> :
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
        _jobService = jobService ?? throw new ArgumentNullException(nameof(jobService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (jobTypeId == Guid.Empty)
            throw new ArgumentException("The job type identifier cannot be empty.", nameof(jobTypeId));

        _jobTypeId = jobTypeId;
        _jobPipe = jobPipe ?? throw new ArgumentNullException(nameof(jobPipe));
    }

    /// <summary>Starts the job when the command targets this registered job type.</summary>
    /// <param name="context">The assigned job and attempt metadata.</param>
    /// <returns>The local admission task, or a completed task for another job type.</returns>
    public Task ConsumeAsync(ConsumeContext<StartJob> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Message.JobTypeId != _jobTypeId)
            return Task.CompletedTask;

        var job = context.GetJob<TJob>() ?? throw new SerializationException($"The job could not be deserialized: {TypeCache<TJob>.ShortName}");

        return _jobService.StartJobAsync(context, job, _jobPipe, _options, context.CancellationToken);
    }
}
