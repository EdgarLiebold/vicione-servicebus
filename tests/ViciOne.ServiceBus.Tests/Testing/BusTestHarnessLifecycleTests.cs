using System.Reflection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class BusTestHarnessLifecycleTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "created-bus-stop-on-start-failure")]
    public async Task StartFailure_StopsTheCreatedBusWithAnOwnedBoundedTokenAsync()
    {
        var expected = new InvalidOperationException("observer connection failed");
        IBusControl bus = DispatchProxy.Create<IBusControl, RecordingBusControlProxy>();
        var proxy = (RecordingBusControlProxy)(object)bus;
        await using var harness = new FailingObserverBusTestHarness(bus, expected)
        {
            TestTimeout = OperationTimeout(),
        };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.StartAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, proxy.StopCount);
        Assert.True(proxy.StopToken.CanBeCanceled);
        Assert.False(proxy.StopToken.IsCancellationRequested);
        Assert.Throws<InvalidOperationException>(() => harness.BusControl);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-LIFECYCLE", "rollback-failure-preserves-start-failure")]
    public async Task RollbackFailure_DoesNotMaskTheOriginalStartFailureAsync()
    {
        var expected = new InvalidOperationException("observer connection failed");
        var rollbackFailure = new InvalidOperationException("bus stop failed");
        IBusControl bus = DispatchProxy.Create<IBusControl, RecordingBusControlProxy>();
        var proxy = (RecordingBusControlProxy)(object)bus;
        proxy.StopFailure = rollbackFailure;
        await using var harness = new FailingObserverBusTestHarness(bus, expected)
        {
            TestTimeout = OperationTimeout(),
        };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.StartAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, proxy.StopCount);
        Assert.Same(rollbackFailure, proxy.StopFailure);
        Assert.Throws<InvalidOperationException>(() => harness.BusControl);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed class FailingObserverBusTestHarness(IBusControl bus, Exception failure) : BusTestHarness
    {
        public override string InputQueueName => "failed-start";

        public override Uri InputQueueAddress => new("loopback://localhost/failed-start");

        protected override Task<IBusControl> CreateBusAsync(CancellationToken cancellationToken) =>
            Task.FromResult(bus);

        protected override void ConnectObservers(IBus createdBus)
        {
            Assert.Same(bus, createdBus);
            throw failure;
        }
    }

    public class RecordingBusControlProxy : DispatchProxy
    {
        public int StopCount { get; private set; }

        public Exception? StopFailure { get; set; }

        public CancellationToken StopToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IBusControl.StopAsync))
            {
                StopCount++;
                StopToken = Assert.IsType<CancellationToken>(Assert.Single(args!));
                return StopFailure == null
                    ? Task.CompletedTask
                    : Task.FromException(StopFailure);
            }

            throw new InvalidOperationException($"Unexpected bus invocation: {targetMethod?.Name}.");
        }
    }
}
