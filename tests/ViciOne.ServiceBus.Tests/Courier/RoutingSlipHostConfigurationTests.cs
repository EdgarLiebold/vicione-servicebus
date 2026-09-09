using System.Collections.Concurrent;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipHostConfigurationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [RequirementCoverage("REQ-VSB-COURIER-HOST-CONFIGURATION", "concurrency-limit-boundaries")]
    public void HostConcurrencyLimits_RejectNonPositiveValues(int value)
    {
        var observer = new NoopActivityConfigurationObserver();
        var execute = new ExecuteActivityHostConfigurator<FirstCourierActivity, CourierArguments>(
            new FactoryMethodExecuteActivityFactory<FirstCourierActivity, CourierArguments>(_ => new FirstCourierActivity()),
            observer);
        var compensate = new CompensateActivityHostConfigurator<FirstCourierActivity, CourierLog>(
            new FactoryMethodCompensateActivityFactory<FirstCourierActivity, CourierLog>(_ => new FirstCourierActivity()),
            observer);

        Assert.Throws<ArgumentOutOfRangeException>(() => execute.ConcurrentMessageLimit = value);
        Assert.Throws<ArgumentOutOfRangeException>(() => compensate.ConcurrentMessageLimit = value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-HOST-CONFIGURATION", "all-execute-and-compensate-pipe-surfaces-run")]
    public async Task ActivityHostConfiguration_ExecutesEveryConfiguredContextSurfaceWithExactDataAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observed = new ConcurrentQueue<string>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-host-configuration");
        var factory = new FactoryMethodActivityFactory<FirstCourierActivity, CourierArguments, CourierLog>(
            _ => new FirstCourierActivity(),
            _ => new FirstCourierActivity());
        var activity = new ActivityTestHarness<FirstCourierActivity, CourierArguments, CourierLog>(
            harness,
            factory,
            execute =>
            {
                execute.UseExecute(context => observed.Enqueue($"execute-host:{context.ActivityName}"));
                execute.ActivityArguments(arguments => arguments.UseExecute(
                    context => observed.Enqueue($"execute-arguments:{context.Arguments.Value}")));
                execute.RoutingSlip(routingSlip => routingSlip.UseExecute(
                    context => observed.Enqueue($"execute-slip:{context.Message.TrackingNumber:D}")));
            },
            compensate =>
            {
                compensate.UseExecute(context => observed.Enqueue($"compensate-host:{context.ActivityName}"));
                compensate.ActivityLog(log => log.UseExecute(
                    context => observed.Enqueue($"compensate-log:{context.Log.OriginalValue}")));
                compensate.RoutingSlip(routingSlip => routingSlip.UseExecute(
                    context => observed.Enqueue($"compensate-slip:{context.Message.TrackingNumber:D}")));
            });
        ExecuteActivityTestHarness<FaultingCourierActivity, FaultingCourierArguments> failing = harness.ExecuteActivity<
            FaultingCourierActivity,
            FaultingCourierArguments>();
        using var compensated = new CourierMessageRecorder<RoutingSlipActivityCompensated>(1);
        using var faulted = new CourierMessageRecorder<RoutingSlipFaulted>(1);
        compensated.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new CourierArguments("configured-value"));
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("terminal"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                compensated.WaitAsync(timeout, cancellationToken),
                faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(6, observed.Count);
            Assert.Contains($"execute-host:{activity.Name}", observed);
            Assert.Contains("execute-arguments:configured-value", observed);
            Assert.Contains($"execute-slip:{trackingNumber:D}", observed);
            Assert.Contains($"compensate-host:{activity.Name}", observed);
            Assert.Contains("compensate-log:configured-value", observed);
            Assert.Contains($"compensate-slip:{trackingNumber:D}", observed);
            Assert.Equal(trackingNumber, Assert.Single(compensated.Messages).Message.TrackingNumber);
            Assert.Equal(trackingNumber, Assert.Single(faulted.Messages).Message.TrackingNumber);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-PARTITION", "shared-partitioner-selects-arguments-and-compensation-log")]
    public async Task SharedPartitioner_UsesTheActivityArgumentsAndCompensationLogSelectorsAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var executeKeys = new ConcurrentQueue<string>();
        var compensateKeys = new ConcurrentQueue<string>();
        IPartitioner? partitioner = null;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-partition");
        harness.OnConfigureBus += _ => partitioner = new PipePartitioner(8);
        var factory = new FactoryMethodActivityFactory<FirstCourierActivity, CourierArguments, CourierLog>(
            _ => new FirstCourierActivity(),
            _ => new FirstCourierActivity());
        var activity = new ActivityTestHarness<FirstCourierActivity, CourierArguments, CourierLog>(
            harness,
            factory,
            execute => execute.UsePartitioner(
                partitioner!,
                context => RecordKey(executeKeys, context.Arguments.Value)),
            compensate => compensate.UsePartitioner(
                partitioner!,
                context => RecordKey(compensateKeys, context.Log.OriginalValue)));
        ExecuteActivityTestHarness<FaultingCourierActivity, FaultingCourierArguments> failing = harness.ExecuteActivity<
            FaultingCourierActivity,
            FaultingCourierArguments>();
        using var compensated = new CourierMessageRecorder<RoutingSlipActivityCompensated>(1);
        using var faulted = new CourierMessageRecorder<RoutingSlipFaulted>(1);
        compensated.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new CourierArguments("partition-key"));
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("terminal"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                compensated.WaitAsync(timeout, cancellationToken),
                faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(["partition-key"], executeKeys);
            Assert.Equal(["partition-key"], compensateKeys);
            Assert.Equal(trackingNumber, Assert.Single(compensated.Messages).Message.TrackingNumber);
            Assert.Equal(trackingNumber, Assert.Single(faulted.Messages).Message.TrackingNumber);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static string RecordKey(ConcurrentQueue<string> keys, string key)
    {
        keys.Enqueue(key);
        return key;
    }

    private sealed class NoopActivityConfigurationObserver : IActivityConfigurationObserver
    {
        public void ActivityConfigured<TActivity, TArguments>(
            IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator,
            Uri compensateAddress)
            where TActivity : class
            where TArguments : class
        {
        }

        public void ExecuteActivityConfigured<TActivity, TArguments>(
            IExecuteActivityPipeConfigurator<TActivity, TArguments> configurator)
            where TActivity : class
            where TArguments : class
        {
        }

        public void CompensateActivityConfigured<TActivity, TLog>(
            ICompensateActivityPipeConfigurator<TActivity, TLog> configurator)
            where TActivity : class
            where TLog : class
        {
        }
    }
}
