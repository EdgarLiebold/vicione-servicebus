using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Time.Testing;
using Quartz;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Quartz.Tests.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class DirectQuartzSchedulerLifecycleObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "direct-observer-null-guard")]
    public void Constructor_RejectsMissingSettings()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new DirectQuartzSchedulerLifecycleObserver(null!));

        Assert.Equal("settings", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "direct-shared-scheduler-is-rejected")]
    public async Task SharedScheduler_CannotBeAttachedToTwoRunningBusesAsync()
    {
        IScheduler scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
        ISchedulerFactory factory = CreateSchedulerFactory(scheduler);
        var first = new DirectQuartzSchedulerLifecycleObserver(
            CreateSettings(factory, "first-quartz", startScheduler: true, TimeProvider.System));
        var second = new DirectQuartzSchedulerLifecycleObserver(
            CreateSettings(factory, "second-quartz", startScheduler: true, TimeProvider.System));
        IBus firstBus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        IBus secondBus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        await first.PreStartAsync(firstBus);
        ConfigurationException failure = await Assert.ThrowsAsync<ConfigurationException>(() =>
            second.PreStartAsync(secondBus));

        Assert.Contains("already attached", failure.Message, StringComparison.Ordinal);
        Assert.Same(firstBus, scheduler.Context[QuartzSchedulerContextKeys.Bus]);
        await first.PreStopAsync(firstBus);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "direct-shutdown-scheduler-fails-closed")]
    public async Task ShutdownScheduler_IsRejectedBeforeItCanBeAttachedAsync()
    {
        IScheduler scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        schedulerProxy.Status = SchedulerStatus.Shutdown;
        var observer = new DirectQuartzSchedulerLifecycleObserver(
            CreateSettings(CreateSchedulerFactory(scheduler), "shutdown-quartz", startScheduler: true, TimeProvider.System));
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        ConfigurationException failure = await Assert.ThrowsAsync<ConfigurationException>(() =>
            observer.PreStartAsync(bus));

        Assert.Contains("shut down", failure.Message, StringComparison.Ordinal);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        Assert.Equal(0, schedulerProxy.StartCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "direct-detach-preserves-a-replacement-owner")]
    public async Task Stop_PreservesSchedulerContextThatWasReassignedToAnotherOwnerAsync()
    {
        IScheduler scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        var observer = new DirectQuartzSchedulerLifecycleObserver(
            CreateSettings(CreateSchedulerFactory(scheduler), "reassigned-quartz", startScheduler: true, TimeProvider.System));
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        IBus replacementOwner = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        await observer.PreStartAsync(bus);
        schedulerProxy.Context[QuartzSchedulerContextKeys.Bus] = replacementOwner;

        await observer.PreStopAsync(bus);

        Assert.Same(replacementOwner, schedulerProxy.Context[QuartzSchedulerContextKeys.Bus]);
        Assert.Same(TimeProvider.System, schedulerProxy.Context[QuartzSchedulerContextKeys.TimeProvider]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "direct-observer-readiness-and-cleanup")]
    public async Task EnabledObserver_WaitsForReadinessAndCleansSchedulerContextAsync()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2042, 2, 3, 4, 5, 6, TimeSpan.Zero));
        IScheduler scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory(scheduler);
        QuartzSchedulerSettings settings = CreateSettings(
            schedulerFactory,
            "direct-quartz",
            startScheduler: true,
            timeProvider);
        var observer = new DirectQuartzSchedulerLifecycleObserver(settings);
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        var readiness = new TaskCompletionSource<BusReady>(TaskCreationOptions.RunContinuationsAsynchronously);

        observer.PostCreate(bus);
        observer.CreateFaulted(new InvalidOperationException("creation-failure"));
        await observer.PreStartAsync(bus);
        Task start = observer.PostStartAsync(bus, readiness.Task);

        Assert.Same(bus, schedulerProxy.Context[QuartzSchedulerContextKeys.Bus]);
        Assert.Same(timeProvider, schedulerProxy.Context[QuartzSchedulerContextKeys.TimeProvider]);
        Assert.False(start.IsCompleted);
        Assert.Equal(0, schedulerProxy.StartCalls);

        readiness.SetResult(DispatchProxy.Create<BusReady, NoOpDispatchProxy>());
        await start;
        await observer.PreStopAsync(bus);
        await observer.PostStopAsync(bus);
        await observer.PostStopAsync(bus);
        await observer.StartFaultedAsync(bus, new InvalidOperationException("start-failure"));
        await observer.StopFaultedAsync(bus, new InvalidOperationException("stop-failure"));

        Assert.Equal(1, schedulerProxy.StartCalls);
        Assert.Equal(1, schedulerProxy.StandbyCalls);
        Assert.Equal(0, schedulerProxy.ShutdownCalls);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "direct-delayed-start")]
    public async Task DelayedObserver_StartsWithTheConfiguredDelayAfterReadinessAsync()
    {
        TimeSpan startDelay = TimeSpan.FromSeconds(17);
        IScheduler scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        QuartzSchedulerSettings settings = CreateSettings(
            CreateSchedulerFactory(scheduler),
            "delayed-quartz",
            startScheduler: true,
            TimeProvider.System,
            startDelay);
        var observer = new DirectQuartzSchedulerLifecycleObserver(settings);
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        await observer.PreStartAsync(bus);
        await observer.PostStartAsync(
            bus,
            Task.FromResult(DispatchProxy.Create<BusReady, NoOpDispatchProxy>()));

        Assert.Equal(0, schedulerProxy.StartCalls);
        Assert.Equal(1, schedulerProxy.StartDelayedCalls);
        Assert.Equal(startDelay, schedulerProxy.StartDelay);
        await observer.PreStopAsync(bus);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "direct-observer-disabled-start")]
    public async Task DisabledObserver_InitializesWithoutWaitingOrStartingAsync()
    {
        IScheduler scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        QuartzSchedulerSettings settings = CreateSettings(
            CreateSchedulerFactory(scheduler),
            "disabled-quartz",
            startScheduler: false,
            TimeProvider.System);
        var observer = new DirectQuartzSchedulerLifecycleObserver(settings);
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        var readiness = new TaskCompletionSource<BusReady>(TaskCreationOptions.RunContinuationsAsynchronously);

        await observer.PreStartAsync(bus);
        await observer.PostStartAsync(bus, readiness.Task);
        await observer.PreStopAsync(bus);
        await observer.PostStopAsync(bus);

        Assert.False(readiness.Task.IsCompleted);
        Assert.Equal(0, schedulerProxy.StartCalls);
        Assert.Equal(0, schedulerProxy.StandbyCalls);
        Assert.Equal(0, schedulerProxy.ShutdownCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "direct-observer-idempotent-stop")]
    public async Task RepeatedStop_DetachesWithoutShuttingDownTheCallerOwnedSchedulerAsync()
    {
        IScheduler scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        QuartzSchedulerSettings settings = CreateSettings(
            CreateSchedulerFactory(scheduler),
            "failing-quartz",
            startScheduler: true,
            TimeProvider.System);
        var observer = new DirectQuartzSchedulerLifecycleObserver(settings);
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        await observer.PreStartAsync(bus);
        await observer.PreStopAsync(bus);
        await observer.PostStopAsync(bus);
        await observer.PreStopAsync(bus);
        await observer.PostStopAsync(bus);

        Assert.Equal(1, schedulerProxy.StandbyCalls);
        Assert.Equal(0, schedulerProxy.ShutdownCalls);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "direct-start-failure-detaches-context")]
    public async Task StartFailure_DetachesWithoutShuttingDownTheCallerOwnedSchedulerAsync()
    {
        var startFailure = new InvalidOperationException("start-failure");
        IScheduler scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        schedulerProxy.StartFailure = startFailure;
        QuartzSchedulerSettings settings = CreateSettings(
            CreateSchedulerFactory(scheduler),
            "failing-start-quartz",
            startScheduler: true,
            TimeProvider.System);
        var observer = new DirectQuartzSchedulerLifecycleObserver(settings);
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        await observer.PreStartAsync(bus);
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            observer.PostStartAsync(bus, Task.FromResult(DispatchProxy.Create<BusReady, NoOpDispatchProxy>())));
        await observer.StartFaultedAsync(bus, failure);
        await observer.PostStopAsync(bus);

        Assert.Same(startFailure, failure);
        Assert.Equal(0, schedulerProxy.ShutdownCalls);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "direct-stop-failure-detaches-context")]
    public async Task StandbyFailure_DetachesAndMakesStopFaultCallbackIdempotentAsync()
    {
        var standbyFailure = new InvalidOperationException("standby-failure");
        IScheduler scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        schedulerProxy.StandbyFailure = standbyFailure;
        QuartzSchedulerSettings settings = CreateSettings(
            CreateSchedulerFactory(scheduler),
            "failing-stop-quartz",
            startScheduler: true,
            TimeProvider.System);
        var observer = new DirectQuartzSchedulerLifecycleObserver(settings);
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        await observer.PreStartAsync(bus);
        await observer.PostStartAsync(bus, Task.FromResult(DispatchProxy.Create<BusReady, NoOpDispatchProxy>()));
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            observer.PreStopAsync(bus));
        await observer.StopFaultedAsync(bus, failure);
        await observer.PostStopAsync(bus);

        Assert.Same(standbyFailure, failure);
        Assert.Equal(0, schedulerProxy.ShutdownCalls);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "starts-and-stops-with-bus")]
    public async Task EnabledScheduler_StartsAndStopsWithTheBusAsync()
    {
        TimeSpan timeout = OperationTimeout();
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory();
        QuartzSchedulerLease? schedulerLease = null;
        IBusControl? bus = null;

        try
        {
            bus = Bus.Factory.CreateUsingInMemory(configurator =>
                schedulerLease = configurator.ConfigureQuartzScheduler(schedulerFactory, options =>
                    options.QueueName = $"quartz-{NewId.NextGuid():N}"));

            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            IScheduler scheduler = await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Equal(SchedulerStatus.Running, scheduler.Status);

            await bus.StopAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            Assert.Equal(SchedulerStatus.Standby, scheduler.Status);

            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            Assert.Equal(SchedulerStatus.Running, scheduler.Status);
            await bus.StopAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            bus = null;

            Assert.Equal(SchedulerStatus.Standby, scheduler.Status);
        }
        finally
        {
            if (bus is not null)
            {
                await bus.StopAsync(CancellationToken.None)
                    .WaitAsync(timeout, CancellationToken.None);
            }
            if (schedulerLease is not null)
                await schedulerLease.DisposeAsync();
            await Assert.IsAssignableFrom<IAsyncDisposable>(schedulerFactory).DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "initializes-without-starting")]
    public async Task DisabledScheduler_IsInitializedButNotStartedAsync()
    {
        TimeSpan timeout = OperationTimeout();
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory();
        QuartzSchedulerLease? schedulerLease = null;
        IBusControl? bus = null;

        try
        {
            bus = Bus.Factory.CreateUsingInMemory(configurator =>
                schedulerLease = configurator.ConfigureQuartzScheduler(schedulerFactory, options =>
                {
                    options.QueueName = $"quartz-{NewId.NextGuid():N}";
                    options.StartScheduler = false;
                }));

            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            IScheduler scheduler = await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Equal(SchedulerStatus.Created, scheduler.Status);

            await bus.StopAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            bus = null;

            Assert.Equal(SchedulerStatus.Created, scheduler.Status);
        }
        finally
        {
            if (bus is not null)
            {
                await bus.StopAsync(CancellationToken.None)
                    .WaitAsync(timeout, CancellationToken.None);
            }
            if (schedulerLease is not null)
                await schedulerLease.DisposeAsync();
            await Assert.IsAssignableFrom<IAsyncDisposable>(schedulerFactory).DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "standalone-context-receives-snapshotted-clock")]
    public async Task StandaloneContext_ReceivesTheConfiguredBusAndTimeProviderAsync()
    {
        TimeSpan timeout = OperationTimeout();
        ISchedulerFactory schedulerFactory = CreateSchedulerFactory();
        QuartzSchedulerLease? schedulerLease = null;
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2042, 2, 3, 4, 5, 6, TimeSpan.Zero));
        IBusControl? bus = null;

        try
        {
            bus = Bus.Factory.CreateUsingInMemory(configurator =>
                schedulerLease = configurator.ConfigureQuartzScheduler(schedulerFactory, options =>
                {
                    options.QueueName = $"quartz-{NewId.NextGuid():N}";
                    options.TimeProvider = timeProvider;
                }));

            await bus.StartAsync(TestContext.Current.CancellationToken)
                .WaitAsync(timeout, TestContext.Current.CancellationToken);
            IScheduler scheduler = await schedulerFactory.GetScheduler(TestContext.Current.CancellationToken).AsTask()
                .WaitAsync(timeout, TestContext.Current.CancellationToken);

            Assert.Same(bus, scheduler.Context[QuartzSchedulerContextKeys.Bus]);
            Assert.Same(timeProvider, scheduler.Context[QuartzSchedulerContextKeys.TimeProvider]);
        }
        finally
        {
            if (bus is not null)
            {
                await bus.StopAsync(CancellationToken.None)
                    .WaitAsync(timeout, CancellationToken.None);
            }
            if (schedulerLease is not null)
                await schedulerLease.DisposeAsync();
            await Assert.IsAssignableFrom<IAsyncDisposable>(schedulerFactory).DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-SETTINGS", "direct-endpoint-qos-applied")]
    public async Task DirectEndpoint_AppliesPrefetchConcurrencyAndPartitionCountAsync()
    {
        TimeSpan timeout = OperationTimeout();
        await using QuartzTestBus fixture = await QuartzTestBus.StartAsync(
            timeout,
            configureScheduler: options =>
            {
                options.PrefetchCount = 19;
                options.ConcurrentMessageLimit = 7;
            });
        JsonNode probe = JsonNode.Parse(JsonSerializer.Serialize(
            fixture.Bus.GetProbeResult(TestContext.Current.CancellationToken).Results))!;
        JsonObject endpoint = Assert.Single(
            NodesIn(probe).OfType<JsonObject>(),
            node => node["name"]?.GetValue<string>() == fixture.QueueName
                && node["receiveTransport"] is JsonObject);
        var transport = Assert.IsType<JsonObject>(endpoint["receiveTransport"]);

        Assert.Equal(19, transport["prefetchCount"]?.GetValue<int>());
        Assert.Equal(7, transport["concurrentMessageLimit"]?.GetValue<int>());
        Assert.Contains(NodesIn(endpoint).OfType<JsonObject>(), node =>
            node["partitionCount"]?.GetValue<int>() == 7);
    }

    private static ISchedulerFactory CreateSchedulerFactory()
    {
        return QuartzSchedulerBuilder.Create()
            .UseProperties(new System.Collections.Specialized.NameValueCollection
            {
                ["quartz.scheduler.instanceName"] = $"ViciOne.ServiceBus.Tests-{NewId.NextGuid():N}",
                ["quartz.threadPool.maxConcurrency"] = "1",
            })
            .Build();
    }

    private static QuartzSchedulerSettings CreateSettings(
        ISchedulerFactory schedulerFactory,
        string queueName,
        bool startScheduler,
        TimeProvider timeProvider,
        TimeSpan? startDelay = null)
    {
        return new QuartzSchedulerSettings(
            schedulerFactory,
            queueName,
            32,
            null,
            startScheduler,
            startDelay,
            true,
            timeProvider,
            null,
            RetryPolicy.Fixed(1, TimeSpan.Zero),
            QuartzSchedulerNamespace.ForEndpoint(queueName));
    }

    private static ISchedulerFactory CreateSchedulerFactory(IScheduler scheduler)
    {
        ISchedulerFactory factory = DispatchProxy.Create<ISchedulerFactory, SchedulerFactoryProxy>();
        ((SchedulerFactoryProxy)(object)factory).Scheduler = scheduler;
        return factory;
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static IEnumerable<JsonNode> NodesIn(JsonNode node)
    {
        yield return node;
        if (node is JsonObject jsonObject)
        {
            foreach ((_, JsonNode? value) in jsonObject)
            {
                if (value is not null)
                {
                    foreach (JsonNode nested in NodesIn(value))
                        yield return nested;
                }
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (JsonNode? value in jsonArray)
            {
                if (value is not null)
                {
                    foreach (JsonNode nested in NodesIn(value))
                        yield return nested;
                }
            }
        }
    }

    private class SchedulerFactoryProxy : DispatchProxy
    {
        public IScheduler? Scheduler { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ISchedulerFactory.GetScheduler))
            {
                return new ValueTask<IScheduler>(
                    Scheduler ?? throw new InvalidOperationException("The scheduler was not configured."));
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class SchedulerProxy : DispatchProxy
    {
        public SchedulerContext Context { get; } = new();
        public int ShutdownCalls { get; private set; }
        public Exception? ShutdownFailure { get; set; }
        public Exception? StandbyFailure { get; set; }
        public int StandbyCalls { get; private set; }
        public Exception? StartFailure { get; set; }
        public int StartCalls { get; private set; }
        public TimeSpan? StartDelay { get; private set; }
        public int StartDelayedCalls { get; private set; }
        public SchedulerStatus Status { get; set; } = SchedulerStatus.Created;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case "get_Context":
                    return Context;
                case "get_SchedulerName":
                    return "direct-quartz";
                case "get_SchedulerInstanceId":
                    return "direct-quartz-instance";
                case "get_Status":
                    return Status;
                case nameof(IScheduler.Start):
                    StartCalls++;
                    if (StartFailure is null)
                        Status = SchedulerStatus.Running;
                    return StartFailure is null
                        ? ValueTask.CompletedTask
                        : ValueTask.FromException(StartFailure);
                case nameof(IScheduler.StartDelayed):
                    StartDelayedCalls++;
                    StartDelay = Assert.IsType<TimeSpan>(args![0]);
                    if (StartFailure is null)
                        Status = SchedulerStatus.Running;
                    return StartFailure is null
                        ? ValueTask.CompletedTask
                        : ValueTask.FromException(StartFailure);
                case nameof(IScheduler.Standby):
                    StandbyCalls++;
                    if (StandbyFailure is null)
                        Status = SchedulerStatus.Standby;
                    return StandbyFailure is null
                        ? ValueTask.CompletedTask
                        : ValueTask.FromException(StandbyFailure);
                case nameof(IScheduler.Shutdown):
                    ShutdownCalls++;
                    if (ShutdownFailure is null)
                        Status = SchedulerStatus.Shutdown;
                    return ShutdownFailure is null
                        ? ValueTask.CompletedTask
                        : ValueTask.FromException(ShutdownFailure);
                default:
                    throw new NotSupportedException(targetMethod?.Name);
            }
        }
    }

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }
}
