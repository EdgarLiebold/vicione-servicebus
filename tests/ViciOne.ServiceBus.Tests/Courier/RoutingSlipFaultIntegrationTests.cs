using System.Collections.Concurrent;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipFaultIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-FAULT", "thrown-exception-capture-and-complete-compensation")]
    public async Task ThrownActivityException_IsCapturedAfterEveryCompletedActivityIsCompensatedAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var compensationOrder = new ConcurrentQueue<string>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-thrown-fault");
        ActivityTestHarness<FirstCourierActivity, CourierArguments, CourierLog> first = harness.Activity<
            FirstCourierActivity,
            CourierArguments,
            CourierLog>(_ => new FirstCourierActivity(compensationOrder), _ => new FirstCourierActivity(compensationOrder));
        ActivityTestHarness<SecondCourierActivity, CourierArguments, CourierLog> second = harness.Activity<
            SecondCourierActivity,
            CourierArguments,
            CourierLog>(_ => new SecondCourierActivity(compensationOrder), _ => new SecondCourierActivity(compensationOrder));
        ExecuteActivityTestHarness<ThrowingCourierActivity, FaultingCourierArguments> throwing = harness.ExecuteActivity<
            ThrowingCourierActivity,
            FaultingCourierArguments>();
        using var activityCompleted = new CourierMessageRecorder<RoutingSlipActivityCompleted>(2);
        using var compensated = new CourierMessageRecorder<RoutingSlipActivityCompensated>(2);
        using var faulted = new CourierMessageRecorder<RoutingSlipFaulted>(1);
        activityCompleted.Configure(harness);
        compensated.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(first.Name, first.ExecuteAddress, new CourierArguments("first"));
            builder.AddActivity(second.Name, second.ExecuteAddress, new CourierArguments("second"));
            builder.AddActivity(throwing.Name, throwing.ExecuteAddress, new FaultingCourierArguments("thrown-courier-failure"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                activityCompleted.WaitAsync(timeout, cancellationToken),
                compensated.WaitAsync(timeout, cancellationToken),
                faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(2, activityCompleted.Count);
            Assert.Equal(2, compensated.Count);
            Assert.Equal(["second:second", "first:first"], compensationOrder);
            Assert.Equal(
                "second",
                Assert.Single(compensated.Messages, context => context.Message.ActivityName == second.Name)
                    .GetResult<string>(nameof(CourierLog.OriginalValue)));
            Assert.Equal(
                "first",
                Assert.Single(compensated.Messages, context => context.Message.ActivityName == first.Name)
                    .GetResult<string>(nameof(CourierLog.OriginalValue)));
            ConsumeContext<RoutingSlipFaulted> slipFailure = Assert.Single(faulted.Messages);
            Assert.Equal(trackingNumber, slipFailure.Message.TrackingNumber);
            ExceptionInfo actual = Assert.Single(slipFailure.Message.ActivityExceptions).ExceptionInfo;
            Assert.Equal(TypeCache<CourierExpectedException>.ShortName, actual.ExceptionType);
            Assert.Equal("thrown-courier-failure", actual.Message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-FAULT", "reverse-compensation-and-complete-event-shape")]
    public async Task ThirdActivityFailure_CompensatesBothCompletedActivitiesInReverseOrderAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var compensationOrder = new ConcurrentQueue<string>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-fault");
        ActivityTestHarness<FirstCourierActivity, CourierArguments, CourierLog> first = harness.Activity<
            FirstCourierActivity,
            CourierArguments,
            CourierLog>(_ => new FirstCourierActivity(compensationOrder), _ => new FirstCourierActivity(compensationOrder));
        ActivityTestHarness<SecondCourierActivity, CourierArguments, CourierLog> second = harness.Activity<
            SecondCourierActivity,
            CourierArguments,
            CourierLog>(_ => new SecondCourierActivity(compensationOrder), _ => new SecondCourierActivity(compensationOrder));
        ExecuteActivityTestHarness<FaultingCourierActivity, FaultingCourierArguments> failing = harness.ExecuteActivity<
            FaultingCourierActivity,
            FaultingCourierArguments>();
        using var activityCompleted = new CourierMessageRecorder<RoutingSlipActivityCompleted>(2);
        using var activityFaulted = new CourierMessageRecorder<RoutingSlipActivityFaulted>(1);
        using var compensated = new CourierMessageRecorder<RoutingSlipActivityCompensated>(2);
        using var faulted = new CourierMessageRecorder<RoutingSlipFaulted>(1);
        activityCompleted.Configure(harness);
        activityFaulted.Configure(harness);
        compensated.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(first.Name, first.ExecuteAddress, new CourierArguments("first"));
            builder.AddActivity(second.Name, second.ExecuteAddress, new CourierArguments("second"));
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("expected-courier-failure"));
            builder.AddVariable("SlipVariable", "knife");

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                activityCompleted.WaitAsync(timeout, cancellationToken),
                activityFaulted.WaitAsync(timeout, cancellationToken),
                compensated.WaitAsync(timeout, cancellationToken),
                faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(new[] { "second:second", "first:first" }, compensationOrder.ToArray());
            Assert.Equal(2, activityCompleted.Count);
            Assert.Equal(2, compensated.Count);
            Assert.Equal(
                "second",
                Assert.Single(compensated.Messages, context => context.Message.ActivityName == second.Name)
                    .GetResult<string>(nameof(CourierLog.OriginalValue)));
            Assert.Equal(
                "first",
                Assert.Single(compensated.Messages, context => context.Message.ActivityName == first.Name)
                    .GetResult<string>(nameof(CourierLog.OriginalValue)));
            Assert.All(activityCompleted.Messages, context => Assert.Equal(trackingNumber, context.Message.TrackingNumber));
            Assert.All(compensated.Messages, context => Assert.Equal(trackingNumber, context.Message.TrackingNumber));
            ConsumeContext<RoutingSlipActivityFaulted> activityFailure = Assert.Single(activityFaulted.Messages);
            Assert.Equal(failing.Name, activityFailure.Message.ActivityName);
            Assert.Contains("expected-courier-failure", activityFailure.Message.ExceptionInfo.Message);
            Assert.Equal("fault-output", activityFailure.GetVariable<string>("FaultVariable"));
            ConsumeContext<RoutingSlipFaulted> slipFailure = Assert.Single(faulted.Messages);
            Assert.Equal(trackingNumber, slipFailure.Message.TrackingNumber);
            Assert.Equal("knife", slipFailure.GetVariable<string>("SlipVariable"));
            Assert.Equal("fault-output", slipFailure.GetVariable<string>("FaultVariable"));
            Assert.Single(slipFailure.Message.ActivityExceptions);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }
}
