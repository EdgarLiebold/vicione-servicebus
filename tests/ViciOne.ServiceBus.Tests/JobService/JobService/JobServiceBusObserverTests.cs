using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Contracts.JobService;
using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.JobService;

public sealed class JobServiceBusObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-BUS-LIFECYCLE", "readiness-start-and-pre-stop-ordering")]
    public async Task Observer_StartsAfterReadinessAndStopsBeforeTheBusAsync()
    {
        var service = new RecordingJobService();
        var observer = new JobServiceBusObserver(service);
        IBus bus = DispatchProxy.Create<IBus, PassiveProxy>();
        BusReady ready = DispatchProxy.Create<BusReady, PassiveProxy>();
        var readiness = new TaskCompletionSource<BusReady>(TaskCreationOptions.RunContinuationsAsynchronously);

        observer.PostCreate(bus);
        await observer.PreStartAsync(bus);
        Task starting = observer.PostStartAsync(bus, readiness.Task);
        Assert.False(starting.IsCompleted);
        Assert.Equal(0, service.StartCount);

        readiness.SetResult(ready);
        await starting;
        Assert.Equal(1, service.StartCount);
        Assert.Same(bus, service.PublishEndpoint);

        await observer.PreStopAsync(bus);
        await observer.PostStopAsync(bus);
        Assert.Equal(1, service.StopCount);
        Assert.Same(bus, service.PublishEndpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-BUS-LIFECYCLE", "lifecycle-failures-propagate-without-hidden-state")]
    public async Task Observer_PropagatesRuntimeAndReadinessFailuresAsync()
    {
        IBus bus = DispatchProxy.Create<IBus, PassiveProxy>();
        var startFailure = new InvalidOperationException("start failed");
        var startService = new RecordingJobService { StartFailure = startFailure };
        var startObserver = new JobServiceBusObserver(startService);

        Exception actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            startObserver.PostStartAsync(bus, Task.FromResult(DispatchProxy.Create<BusReady, PassiveProxy>())));
        Assert.Same(startFailure, actual);

        var readinessFailure = new InvalidOperationException("readiness failed");
        actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            startObserver.PostStartAsync(bus, Task.FromException<BusReady>(readinessFailure)));
        Assert.Same(readinessFailure, actual);
        Assert.Equal(1, startService.StartCount);

        var stopFailure = new InvalidOperationException("stop failed");
        var stopObserver = new JobServiceBusObserver(new RecordingJobService { StopFailure = stopFailure });
        actual = await Assert.ThrowsAsync<InvalidOperationException>(() => stopObserver.PreStopAsync(bus));
        Assert.Same(stopFailure, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-BUS-LIFECYCLE", "all-required-observer-inputs-are-validated")]
    public async Task Observer_ValidatesEveryRequiredLifecycleInputAsync()
    {
        Assert.Equal("jobService", Assert.Throws<ArgumentNullException>(() => new JobServiceBusObserver(null!)).ParamName);
        var observer = new JobServiceBusObserver(new RecordingJobService());
        IBus bus = DispatchProxy.Create<IBus, PassiveProxy>();
        var failure = new InvalidOperationException("expected");

        Assert.Equal("bus", Assert.Throws<ArgumentNullException>(() => observer.PostCreate(null!)).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() => observer.CreateFaulted(null!)).ParamName);
        observer.CreateFaulted(failure);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() => observer.PreStartAsync(null!))).ParamName);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            observer.PostStartAsync(null!, Task.FromResult(DispatchProxy.Create<BusReady, PassiveProxy>())))).ParamName);
        Assert.Equal("busReady", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            observer.PostStartAsync(bus, null!))).ParamName);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            observer.StartFaultedAsync(null!, failure))).ParamName);
        Assert.Equal("exception", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            observer.StartFaultedAsync(bus, null!))).ParamName);
        await observer.StartFaultedAsync(bus, failure);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() => observer.PreStopAsync(null!))).ParamName);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() => observer.PostStopAsync(null!))).ParamName);
        await observer.PostStopAsync(bus);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            observer.StopFaultedAsync(null!, failure))).ParamName);
        Assert.Equal("exception", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            observer.StopFaultedAsync(bus, null!))).ParamName);
        await observer.StopFaultedAsync(bus, failure);
    }

    private sealed class RecordingJobService : IJobService
    {
        public Exception? StartFailure { get; init; }

        public Exception? StopFailure { get; init; }

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public IPublishEndpoint? PublishEndpoint { get; private set; }

        public Uri InstanceAddress { get; } = new("loopback://localhost/job-service");

        public JobServiceSettings Settings => throw new NotSupportedException();

        public Task BusStartedAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default)
        {
            StartCount++;
            PublishEndpoint = publishEndpoint;
            return StartFailure is null ? Task.CompletedTask : Task.FromException(StartFailure);
        }

        public Task StopAsync(IPublishEndpoint publishEndpoint, CancellationToken cancellationToken = default)
        {
            StopCount++;
            PublishEndpoint = publishEndpoint;
            return StopFailure is null ? Task.CompletedTask : Task.FromException(StopFailure);
        }

        public Task StartJobAsync<TJob>(
            ConsumeContext<StartJob> context,
            TJob job,
            IPipe<ConsumeContext<TJob>> jobPipe,
            JobOptions<TJob> jobOptions,
            CancellationToken cancellationToken = default)
            where TJob : class => throw new NotSupportedException();

        public bool TryGetJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobReference) =>
            throw new NotSupportedException();

        public bool TryRemoveJob(Guid jobId, [NotNullWhen(true)] out JobHandle? jobHandle) =>
            throw new NotSupportedException();

        public void RegisterJobType<TJob>(JobOptions<TJob> options, Guid jobTypeId, string jobTypeName)
            where TJob : class => throw new NotSupportedException();

        public Guid GetJobTypeId<TJob>()
            where TJob : class => throw new NotSupportedException();

        public void ConfigureSuperviseJobConsumer(IReceiveEndpointConfigurator configurator) =>
            throw new NotSupportedException();
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
