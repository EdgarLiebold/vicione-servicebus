using ViciOne.ServiceBus.Contracts.JobService;

namespace ViciOne.ServiceBus.Tests.JobService.Integration.ContainerJobDiscovery;

public sealed class DiscoveryMarker;

public sealed record CrunchNumbers(Guid CorrelationId, int Value) : ICorrelatedBy<Guid>;

public sealed record JobSnapshot(Guid CorrelationId, Guid JobId, int Value);

public sealed class JobObservation
{
    public TaskCompletionSource<JobSnapshot> Executed { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class CrunchNumbersConsumer(JobObservation observation) : IJobConsumer<CrunchNumbers>
{
    public Task RunAsync(JobContext<CrunchNumbers> context)
    {
        observation.Executed.TrySetResult(new JobSnapshot(
            context.Job.CorrelationId,
            context.JobId,
            context.Job.Value));
        return Task.CompletedTask;
    }
}
