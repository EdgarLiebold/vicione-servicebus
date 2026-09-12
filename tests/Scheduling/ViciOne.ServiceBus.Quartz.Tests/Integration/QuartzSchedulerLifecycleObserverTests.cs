using System.Reflection;
using Quartz;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Integration;

public sealed class QuartzSchedulerLifecycleObserverTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "container-observer-null-guard")]
    public void Constructor_RejectsAMissingBinding()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new QuartzSchedulerLifecycleObserver<IBus>(null!));

        Assert.Equal("binding", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "readiness-stop-restart")]
    public async Task Lifecycle_AwaitsReadinessDetachesOnStopAndSupportsRestartAsync()
    {
        TimeSpan delay = TimeSpan.FromSeconds(17);
        var timeProvider = new TimeProviderProxy();
        IScheduler scheduler = CreateScheduler();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        var binding = CreateBinding(
            scheduler,
            timeProvider,
            options => options.StartDelay = delay);
        var observer = new QuartzSchedulerLifecycleObserver<IBus>(binding);
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        var readiness = new TaskCompletionSource<BusReady>(TaskCreationOptions.RunContinuationsAsynchronously);

        observer.PostCreate(bus);
        observer.CreateFaulted(new InvalidOperationException("creation-failure"));
        await observer.PreStartAsync(bus);
        Task firstStart = observer.PostStartAsync(bus, readiness.Task);

        Assert.False(firstStart.IsCompleted);
        Assert.Equal(0, schedulerProxy.StartCalls);

        readiness.SetResult(DispatchProxy.Create<BusReady, NoOpDispatchProxy>());
        await firstStart;

        Assert.Equal(delay, schedulerProxy.StartDelay);
        Assert.Same(bus, schedulerProxy.Context[QuartzSchedulerContextKeys.Bus]);
        Assert.Same(timeProvider, schedulerProxy.Context[QuartzSchedulerContextKeys.TimeProvider]);

        await observer.PreStopAsync(bus);
        await observer.PostStopAsync(bus);

        Assert.Equal(1, schedulerProxy.StandbyCalls);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.TimeProvider));
        Assert.Equal(0, schedulerProxy.ShutdownCalls);

        await observer.PostStartAsync(bus, Task.FromResult(DispatchProxy.Create<BusReady, NoOpDispatchProxy>()));
        await observer.PreStopAsync(bus);

        Assert.Equal(2, schedulerProxy.StartCalls);
        Assert.Equal(2, schedulerProxy.StandbyCalls);
        Assert.Equal(0, schedulerProxy.ShutdownCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "container-start-failure-detaches-context")]
    public async Task StartFailure_DetachesTheBusWithoutShuttingDownTheExternalSchedulerAsync()
    {
        var startFailure = new InvalidOperationException("start-failure");
        IScheduler scheduler = CreateScheduler();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        schedulerProxy.StartFailure = startFailure;
        var observer = new QuartzSchedulerLifecycleObserver<IBus>(
            CreateBinding(scheduler, TimeProvider.System));
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            observer.PostStartAsync(bus, Task.FromResult(DispatchProxy.Create<BusReady, NoOpDispatchProxy>())));
        await observer.StartFaultedAsync(bus, failure);

        Assert.Same(startFailure, failure);
        Assert.Equal(0, schedulerProxy.ShutdownCalls);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "container-stop-failure-detaches-context")]
    public async Task StandbyFailure_DetachesTheBusAndMakesTheFaultCallbackIdempotentAsync()
    {
        var standbyFailure = new InvalidOperationException("standby-failure");
        IScheduler scheduler = CreateScheduler();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        var observer = new QuartzSchedulerLifecycleObserver<IBus>(
            CreateBinding(scheduler, TimeProvider.System));
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        await observer.PostStartAsync(bus, Task.FromResult(DispatchProxy.Create<BusReady, NoOpDispatchProxy>()));
        schedulerProxy.StandbyFailure = standbyFailure;
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            observer.PreStopAsync(bus));
        await observer.StopFaultedAsync(bus, failure);
        await observer.PostStopAsync(bus);

        Assert.Same(standbyFailure, failure);
        Assert.Equal(1, schedulerProxy.StandbyCalls);
        Assert.Equal(0, schedulerProxy.ShutdownCalls);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.TimeProvider));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "factory-has-exactly-one-bus-owner")]
    public void OneFactory_CannotBeClaimedByTwoBusOwners()
    {
        IScheduler scheduler = CreateScheduler();
        ISchedulerFactory factory = CreateSchedulerFactory(scheduler);
        var claims = new QuartzSchedulerClaimRegistry();
        var primary = new QuartzSchedulerBinding<IBus>(
            factory,
            ownsFactory: false,
            new QuartzEndpointOptions { QueueName = "primary-quartz" }.CreateSettings(typeof(IBus)),
            TimeProvider.System,
            claims);

        Assert.Equal(QuartzSchedulerNamespace.GetStableBusIdentity(typeof(IBus)), primary.BusKey);

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            new QuartzSchedulerBinding<ISecondaryBus>(
                factory,
                ownsFactory: false,
                new QuartzEndpointOptions { QueueName = "secondary-quartz" }.CreateSettings(typeof(ISecondaryBus)),
                TimeProvider.System,
                claims));

        Assert.Contains("scheduler factory", failure.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(ISecondaryBus).FullName!, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "scheduler-name-has-exactly-one-bus-owner")]
    public async Task OneSchedulerIdentity_CannotBeClaimedByTwoBusOwnersAsync()
    {
        var claims = new QuartzSchedulerClaimRegistry();
        var primary = new QuartzSchedulerBinding<IBus>(
            CreateSchedulerFactory(CreateScheduler()),
            ownsFactory: false,
            new QuartzEndpointOptions { QueueName = "primary-quartz" }.CreateSettings(typeof(IBus)),
            TimeProvider.System,
            claims);
        var secondary = new QuartzSchedulerBinding<ISecondaryBus>(
            CreateSchedulerFactory(CreateScheduler()),
            ownsFactory: false,
            new QuartzEndpointOptions { QueueName = "secondary-quartz" }.CreateSettings(typeof(ISecondaryBus)),
            TimeProvider.System,
            claims);

        _ = await primary.GetSchedulerAsync(TestContext.Current.CancellationToken);
        ConfigurationException failure = await Assert.ThrowsAsync<ConfigurationException>(() =>
            secondary.GetSchedulerAsync(TestContext.Current.CancellationToken).AsTask());

        Assert.Contains("scheduler name", failure.Message, StringComparison.Ordinal);
        Assert.Contains("test-scheduler", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "owned-binding-shuts-down-only-on-final-disposal")]
    public async Task AdapterOwnedBinding_SupportsRestartAndShutsDownOnlyOnFinalDisposalAsync()
    {
        IScheduler scheduler = CreateScheduler();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        ISchedulerFactory factory = CreateSchedulerFactory(scheduler);
        var factoryProxy = (SchedulerFactoryProxy)(object)factory;
        var binding = new QuartzSchedulerBinding<IBus>(
            factory,
            ownsFactory: true,
            new QuartzEndpointOptions().CreateSettings(typeof(IBus)),
            TimeProvider.System,
            new QuartzSchedulerClaimRegistry());
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        await binding.StartAsync(bus);
        await binding.StandbyAndDetachAsync();
        Assert.Equal(0, schedulerProxy.ShutdownCalls);
        await binding.StartAsync(bus);

        await binding.DisposeAsync();

        Assert.Equal(2, schedulerProxy.StartCalls);
        Assert.Equal(1, schedulerProxy.StandbyCalls);
        Assert.Equal(1, schedulerProxy.ShutdownCalls);
        Assert.Equal(1, factoryProxy.DisposeCalls);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        await binding.DisposeAsync();
        Assert.Equal(1, schedulerProxy.ShutdownCalls);
        Assert.Equal(1, factoryProxy.DisposeCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "external-binding-stands-by-without-disposing-factory")]
    public async Task ExternalBinding_DisposalStandsByDetachesAndRejectsFurtherUseAsync()
    {
        IScheduler scheduler = CreateScheduler();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        ISchedulerFactory factory = CreateSchedulerFactory(scheduler);
        var factoryProxy = (SchedulerFactoryProxy)(object)factory;
        var binding = new QuartzSchedulerBinding<IBus>(
            factory,
            ownsFactory: false,
            new QuartzEndpointOptions().CreateSettings(typeof(IBus)),
            TimeProvider.System,
            new QuartzSchedulerClaimRegistry());
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        await binding.StartAsync(bus);
        await binding.DisposeAsync();

        Assert.Equal(1, schedulerProxy.StandbyCalls);
        Assert.Equal(0, schedulerProxy.ShutdownCalls);
        Assert.Equal(0, factoryProxy.DisposeCalls);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            binding.GetSchedulerAsync(TestContext.Current.CancellationToken).AsTask());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-LIFECYCLE", "container-shutdown-scheduler-fails-closed")]
    public async Task ShutdownScheduler_IsRejectedBeforeItCanBeClaimedOrAttachedAsync()
    {
        IScheduler scheduler = CreateScheduler();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        schedulerProxy.Status = SchedulerStatus.Shutdown;
        var binding = CreateBinding(scheduler, TimeProvider.System);

        ConfigurationException failure = await Assert.ThrowsAsync<ConfigurationException>(() =>
            binding.GetSchedulerAsync(TestContext.Current.CancellationToken).AsTask());

        Assert.Contains("shut down", failure.Message, StringComparison.Ordinal);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "binding-rejects-a-foreign-context-owner")]
    public async Task Binding_CannotAttachASchedulerOwnedByAnotherBusAsync()
    {
        IScheduler scheduler = CreateScheduler();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        IBus foreignOwner = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        schedulerProxy.Context[QuartzSchedulerContextKeys.Bus] = foreignOwner;
        var binding = CreateBinding(scheduler, TimeProvider.System);
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();

        ConfigurationException failure = await Assert.ThrowsAsync<ConfigurationException>(() =>
            binding.StartAsync(bus));

        Assert.Contains("already attached", failure.Message, StringComparison.Ordinal);
        Assert.Same(foreignOwner, schedulerProxy.Context[QuartzSchedulerContextKeys.Bus]);
        Assert.Equal(0, schedulerProxy.StartCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "binding-preserves-a-single-cleanup-failure")]
    public async Task SingleSchedulerCleanupFailure_IsRethrownWithoutWrappingAsync()
    {
        var shutdownFailure = new InvalidOperationException("scheduler-shutdown-failure");
        IScheduler scheduler = CreateScheduler();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        ISchedulerFactory factory = CreateSchedulerFactory(scheduler);
        var binding = new QuartzSchedulerBinding<IBus>(
            factory,
            ownsFactory: true,
            new QuartzEndpointOptions().CreateSettings(typeof(IBus)),
            TimeProvider.System,
            new QuartzSchedulerClaimRegistry());
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        await binding.StartAsync(bus);
        schedulerProxy.ShutdownFailure = shutdownFailure;

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(binding.DisposeAsync().AsTask);

        Assert.Same(shutdownFailure, failure);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "binding-reports-every-cleanup-failure")]
    public async Task IndependentBindingCleanupFailures_AreReportedTogetherAndDetachTheBusAsync()
    {
        var shutdownFailure = new InvalidOperationException("scheduler-shutdown-failure");
        var factoryFailure = new InvalidOperationException("factory-disposal-failure");
        IScheduler scheduler = CreateScheduler();
        var schedulerProxy = (SchedulerProxy)(object)scheduler;
        ISchedulerFactory factory = CreateSchedulerFactory(scheduler);
        var factoryProxy = (SchedulerFactoryProxy)(object)factory;
        var binding = new QuartzSchedulerBinding<IBus>(
            factory,
            ownsFactory: true,
            new QuartzEndpointOptions().CreateSettings(typeof(IBus)),
            TimeProvider.System,
            new QuartzSchedulerClaimRegistry());
        IBus bus = DispatchProxy.Create<IBus, NoOpDispatchProxy>();
        await binding.StartAsync(bus);
        schedulerProxy.ShutdownFailure = shutdownFailure;
        factoryProxy.DisposalFailure = factoryFailure;

        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(binding.DisposeAsync().AsTask);

        Assert.Equal([shutdownFailure, factoryFailure], failure.InnerExceptions);
        Assert.Equal(1, schedulerProxy.ShutdownCalls);
        Assert.Equal(1, factoryProxy.DisposeCalls);
        Assert.False(schedulerProxy.Context.ContainsKey(QuartzSchedulerContextKeys.Bus));
        await binding.DisposeAsync();
    }

    private static QuartzSchedulerBinding<IBus> CreateBinding(
        IScheduler scheduler,
        TimeProvider timeProvider,
        Action<QuartzEndpointOptions>? configure = null)
    {
        var options = new QuartzEndpointOptions();
        configure?.Invoke(options);
        return new QuartzSchedulerBinding<IBus>(
            CreateSchedulerFactory(scheduler),
            ownsFactory: false,
            options.CreateSettings(typeof(IBus)),
            timeProvider,
            new QuartzSchedulerClaimRegistry());
    }

    private static IScheduler CreateScheduler()
    {
        IScheduler scheduler = DispatchProxy.Create<IScheduler, SchedulerProxy>();
        ((SchedulerProxy)(object)scheduler).Status = SchedulerStatus.Created;
        return scheduler;
    }

    private static ISchedulerFactory CreateSchedulerFactory(IScheduler scheduler)
    {
        ISchedulerFactory factory = DispatchProxy.Create<IAsyncSchedulerFactory, SchedulerFactoryProxy>();
        ((SchedulerFactoryProxy)(object)factory).Scheduler = scheduler;
        return factory;
    }

    private interface IAsyncSchedulerFactory : ISchedulerFactory, IAsyncDisposable;

    private class SchedulerFactoryProxy : DispatchProxy
    {
        public IScheduler? Scheduler { get; set; }
        public Exception? DisposalFailure { get; set; }
        public int DisposeCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ISchedulerFactory.GetScheduler))
                return new ValueTask<IScheduler>(Scheduler!);
            if (targetMethod?.Name == nameof(IAsyncDisposable.DisposeAsync))
            {
                DisposeCalls++;
                return DisposalFailure is null
                    ? ValueTask.CompletedTask
                    : ValueTask.FromException(DisposalFailure);
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }

    private class SchedulerProxy : DispatchProxy
    {
        public SchedulerContext Context { get; } = new();
        public TimeSpan? StartDelay { get; private set; }
        public int StartCalls { get; private set; }
        public Exception? StartFailure { get; set; }
        public int StandbyCalls { get; private set; }
        public Exception? StandbyFailure { get; set; }
        public Exception? ShutdownFailure { get; set; }
        public int ShutdownCalls { get; private set; }
        public SchedulerStatus Status { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case "get_Context":
                    return Context;
                case "get_SchedulerName":
                    return "test-scheduler";
                case "get_Status":
                    return Status;
                case nameof(IScheduler.StartDelayed):
                    StartDelay = Assert.IsType<TimeSpan>(args![0]);
                    return StartAsync();
                case nameof(IScheduler.Start):
                    return StartAsync();
                case nameof(IScheduler.Standby):
                    StandbyCalls++;
                    if (StandbyFailure is not null)
                        return ValueTask.FromException(StandbyFailure);
                    Status = SchedulerStatus.Standby;
                    return ValueTask.CompletedTask;
                case nameof(IScheduler.Shutdown):
                    ShutdownCalls++;
                    if (ShutdownFailure is not null)
                        return ValueTask.FromException(ShutdownFailure);
                    Status = SchedulerStatus.Shutdown;
                    return ValueTask.CompletedTask;
                default:
                    throw new NotSupportedException(targetMethod?.Name);
            }
        }

        private ValueTask StartAsync()
        {
            StartCalls++;
            if (StartFailure is not null)
                return ValueTask.FromException(StartFailure);
            Status = SchedulerStatus.Running;
            return ValueTask.CompletedTask;
        }
    }

    private class NoOpDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class TimeProviderProxy : TimeProvider;

    private interface ISecondaryBus : IBus;
}
