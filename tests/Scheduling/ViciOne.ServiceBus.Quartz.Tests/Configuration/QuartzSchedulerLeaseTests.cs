using System.Reflection;
using Quartz;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Quartz.Runtime;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Quartz.Tests.Configuration;

public sealed class QuartzSchedulerLeaseTests
{
    private static readonly Uri EndpointAddress = new("loopback://localhost/quartz-lease");

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "lease-constructor-null-guards")]
    public async Task Constructor_RejectsEveryMissingOwnedResourceAsync()
    {
        ISchedulerFactory factory = QuartzSchedulingExtensions.CreateInMemorySchedulerFactory();
        try
        {
            var lifecycleObserver = new TrackingConnectHandle();
            var partitioner = new TrackingAsyncDisposable();

            Assert.Equal("endpointAddress", Assert.Throws<ArgumentNullException>(() =>
                new QuartzSchedulerLease(null!, factory, true, true, lifecycleObserver, partitioner)).ParamName);
            Assert.Equal("schedulerFactory", Assert.Throws<ArgumentNullException>(() =>
                new QuartzSchedulerLease(EndpointAddress, null!, true, true, lifecycleObserver, partitioner)).ParamName);
            Assert.Equal("lifecycleObserver", Assert.Throws<ArgumentNullException>(() =>
                new QuartzSchedulerLease(EndpointAddress, factory, true, true, null!, partitioner)).ParamName);
            Assert.Equal("partitioner", Assert.Throws<ArgumentNullException>(() =>
                new QuartzSchedulerLease(EndpointAddress, factory, true, true, lifecycleObserver, null!)).ParamName);
        }
        finally
        {
            await Assert.IsAssignableFrom<IAsyncDisposable>(factory).DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "owned-lease-releases-every-resource-idempotently")]
    public async Task AdapterOwnedLease_ReleasesEveryOwnedResourceExactlyOnceAsync()
    {
        ISchedulerFactory factory = QuartzSchedulingExtensions.CreateInMemorySchedulerFactory();
        IScheduler scheduler = await factory.GetScheduler(TestContext.Current.CancellationToken);
        await scheduler.Start(TestContext.Current.CancellationToken);
        var lifecycleObserver = new TrackingConnectHandle();
        var partitioner = new TrackingAsyncDisposable();
        var lease = new QuartzSchedulerLease(
            EndpointAddress,
            factory,
            ownsSchedulerFactory: true,
            waitForJobsToComplete: true,
            lifecycleObserver,
            partitioner);

        await lease.DisposeAsync();
        await lease.DisposeAsync();

        Assert.Equal(SchedulerStatus.Shutdown, scheduler.Status);
        Assert.Equal(1, lifecycleObserver.DisposeCalls);
        Assert.Equal(1, partitioner.DisposeCalls);
        Assert.Throws<ObjectDisposedException>(() => lease.SchedulerFactory);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "caller-owned-lease-never-controls-factory")]
    public async Task CallerOwnedLease_ReleasesOnlyTheBusOwnedPartitionerAsync()
    {
        ISchedulerFactory factory = QuartzSchedulingExtensions.CreateInMemorySchedulerFactory();
        IScheduler scheduler = await factory.GetScheduler(TestContext.Current.CancellationToken);
        await scheduler.Start(TestContext.Current.CancellationToken);
        var lifecycleObserver = new TrackingConnectHandle();
        var partitioner = new TrackingAsyncDisposable();
        var lease = new QuartzSchedulerLease(
            EndpointAddress,
            factory,
            ownsSchedulerFactory: false,
            waitForJobsToComplete: true,
            lifecycleObserver,
            partitioner);

        try
        {
            await lease.DisposeAsync();

            Assert.Equal(SchedulerStatus.Running, scheduler.Status);
            Assert.Equal(1, lifecycleObserver.DisposeCalls);
            Assert.Equal(1, partitioner.DisposeCalls);
            Assert.Throws<ObjectDisposedException>(() => lease.SchedulerFactory);
        }
        finally
        {
            await scheduler.Shutdown(waitForJobsToComplete: true, TestContext.Current.CancellationToken);
            await Assert.IsAssignableFrom<IAsyncDisposable>(factory).DisposeAsync();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "owned-lease-cleans-factory-after-partitioner-failure")]
    public async Task PartitionerDisposalFailure_DoesNotPreventOwnedSchedulerCleanupAsync()
    {
        ISchedulerFactory factory = QuartzSchedulingExtensions.CreateInMemorySchedulerFactory();
        IScheduler scheduler = await factory.GetScheduler(TestContext.Current.CancellationToken);
        await scheduler.Start(TestContext.Current.CancellationToken);
        var disposalFailure = new InvalidOperationException("partitioner-disposal-failure");
        var lifecycleObserver = new TrackingConnectHandle();
        var partitioner = new TrackingAsyncDisposable(disposalFailure);
        var lease = new QuartzSchedulerLease(
            EndpointAddress,
            factory,
            ownsSchedulerFactory: true,
            waitForJobsToComplete: true,
            lifecycleObserver,
            partitioner);

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            lease.DisposeAsync().AsTask);

        Assert.Same(disposalFailure, failure);
        Assert.Equal(SchedulerStatus.Shutdown, scheduler.Status);
        Assert.Equal(1, lifecycleObserver.DisposeCalls);
        Assert.Equal(1, partitioner.DisposeCalls);
        await lease.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "lease-reports-every-cleanup-failure")]
    public async Task IndependentCleanupFailures_AreReportedTogetherAsync()
    {
        var lifecycleFailure = new InvalidOperationException("observer-disconnection-failure");
        var partitionerFailure = new InvalidOperationException("partitioner-disposal-failure");
        var factoryQueryFailure = new InvalidOperationException("factory-query-failure");
        var factoryDisposalFailure = new InvalidOperationException("factory-disposal-failure");
        IAsyncSchedulerFactory factory = DispatchProxy.Create<IAsyncSchedulerFactory, FailingSchedulerFactoryProxy>();
        var factoryProxy = (FailingSchedulerFactoryProxy)(object)factory;
        factoryProxy.QueryFailure = factoryQueryFailure;
        factoryProxy.DisposalFailure = factoryDisposalFailure;
        var lifecycleObserver = new TrackingConnectHandle(lifecycleFailure);
        var partitioner = new TrackingAsyncDisposable(partitionerFailure);
        var lease = new QuartzSchedulerLease(
            EndpointAddress,
            factory,
            ownsSchedulerFactory: true,
            waitForJobsToComplete: true,
            lifecycleObserver,
            partitioner);

        AggregateException failure = await Assert.ThrowsAsync<AggregateException>(lease.DisposeAsync().AsTask);

        Assert.Equal([lifecycleFailure, partitionerFailure, factoryQueryFailure, factoryDisposalFailure], failure.InnerExceptions);
        Assert.Equal(1, lifecycleObserver.DisposeCalls);
        Assert.Equal(1, partitioner.DisposeCalls);
        Assert.Equal(1, factoryProxy.DisposeCalls);
        await lease.DisposeAsync();
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SCHEDULER-OWNERSHIP", "owned-lease-supports-synchronous-factory-disposal")]
    public async Task SynchronouslyDisposableFactory_IsReleasedByTheOwnedLeaseAsync()
    {
        ISyncSchedulerFactory factory = DispatchProxy.Create<ISyncSchedulerFactory, SyncSchedulerFactoryProxy>();
        var factoryProxy = (SyncSchedulerFactoryProxy)(object)factory;
        var lifecycleObserver = new TrackingConnectHandle();
        var lease = new QuartzSchedulerLease(
            EndpointAddress,
            factory,
            ownsSchedulerFactory: true,
            waitForJobsToComplete: true,
            lifecycleObserver,
            new TrackingAsyncDisposable());

        await lease.DisposeAsync();

        Assert.Equal(1, lifecycleObserver.DisposeCalls);
        Assert.Equal(1, factoryProxy.DisposeCalls);
    }

    private sealed class TrackingConnectHandle(Exception? failure = null) : ConnectHandle
    {
        public int DisposeCalls { get; private set; }

        public void Disconnect()
        {
            Dispose();
        }

        public void Dispose()
        {
            DisposeCalls++;
            if (failure is not null)
                throw failure;
        }
    }

    private sealed class TrackingAsyncDisposable(Exception? failure = null) : IAsyncDisposable
    {
        public int DisposeCalls { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            return failure is null ? ValueTask.CompletedTask : ValueTask.FromException(failure);
        }
    }

    private interface IAsyncSchedulerFactory : ISchedulerFactory, IAsyncDisposable;

    private interface ISyncSchedulerFactory : ISchedulerFactory, IDisposable;

    private class FailingSchedulerFactoryProxy : DispatchProxy
    {
        public Exception? QueryFailure { get; set; }
        public Exception? DisposalFailure { get; set; }
        public int DisposeCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                nameof(ISchedulerFactory.GetAllSchedulers) =>
                    ValueTask.FromException<List<IScheduler>>(QueryFailure!),
                nameof(IAsyncDisposable.DisposeAsync) => DisposeAsync(),
                _ => throw new NotSupportedException(targetMethod?.Name),
            };
        }

        private ValueTask DisposeAsync()
        {
            DisposeCalls++;
            return ValueTask.FromException(DisposalFailure!);
        }
    }

    private class SyncSchedulerFactoryProxy : DispatchProxy
    {
        public int DisposeCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(ISchedulerFactory.GetAllSchedulers))
                return ValueTask.FromResult(new List<IScheduler>());
            if (targetMethod?.Name == nameof(IDisposable.Dispose))
            {
                DisposeCalls++;
                return null;
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }
}
