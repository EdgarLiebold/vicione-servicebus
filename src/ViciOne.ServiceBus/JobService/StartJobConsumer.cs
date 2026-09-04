using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.JobService;

public class StartJobConsumer<TJob> :
    IConsumer<StartJob>
    where TJob : class
{
    readonly IPipe<ConsumeContext<TJob>> _jobPipe;
    readonly IJobService _jobService;
    readonly Guid _jobTypeId;
    readonly JobOptions<TJob> _options;

    public StartJobConsumer(IJobService jobService, JobOptions<TJob> options, Guid jobTypeId, IPipe<ConsumeContext<TJob>> jobPipe)
    {
        _jobService = jobService;
        _options = options;
        _jobTypeId = jobTypeId;
        _jobPipe = jobPipe;
    }

    public Task ConsumeAsync(ConsumeContext<StartJob> context)
    {
        if (context.Message.JobTypeId != _jobTypeId)
            return Task.CompletedTask;

        var job = context.GetJob<TJob>() ?? throw new SerializationException($"The job could not be deserialized: {TypeCache<TJob>.ShortName}");

        return _jobService.StartJobAsync(context, job, _jobPipe, _options);
    }
}
