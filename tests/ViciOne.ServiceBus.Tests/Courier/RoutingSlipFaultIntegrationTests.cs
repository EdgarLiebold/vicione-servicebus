using System.Collections.Concurrent;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipFaultIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-FAULT", "fault-delay-defers-compensation")]
    public async Task FaultDelay_DefersCompensationByTheConfiguredDurationAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        TimeSpan delay = TimeSpan.FromMilliseconds(400);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observed = new TaskCompletionSource<DateTimeOffset>(TaskCreationOptions.RunContinuationsAsynchronously);
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-fault-delay");
        harness.OnConfigureInMemoryBus += bus => bus.ConfigureDelayedMessageScheduler();
        ActivityTestHarness<ObservingCompensationCourierActivity, CourierArguments, CourierLog> compensating = harness.Activity<
            ObservingCompensationCourierActivity,
            CourierArguments,
            CourierLog>(_ => new ObservingCompensationCourierActivity(observed), _ => new ObservingCompensationCourierActivity(observed));
        ExecuteActivityTestHarness<DelayedFaultingCourierActivity, DelayedCourierArguments> failing = harness.ExecuteActivity<
            DelayedFaultingCourierActivity,
            DelayedCourierArguments>();
        using var faulted = new CourierMessageRecorder<RoutingSlipFaulted>(1);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddActivity(compensating.Name, compensating.ExecuteAddress, new CourierArguments("compensate"));
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new DelayedCourierArguments(delay));
            DateTimeOffset startedAt = DateTimeOffset.UtcNow;

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            DateTimeOffset observedAt = await observed.Task.WaitAsync(timeout, cancellationToken);
            await faulted.WaitAsync(timeout, cancellationToken);

            Assert.True(observedAt - startedAt >= TimeSpan.FromMilliseconds(250),
                $"Compensation started after {observedAt - startedAt}, before the configured {delay} delay could elapse.");
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(InvalidResultOption.CompletedNullCallback, typeof(ArgumentNullException))]
    [InlineData(InvalidResultOption.CompletedTypedLogNullCallback, typeof(ArgumentNullException))]
    [InlineData(InvalidResultOption.CompletedProjectedLogNullCallback, typeof(ArgumentNullException))]
    [InlineData(InvalidResultOption.FaultedNullCallback, typeof(ArgumentNullException))]
    [InlineData(InvalidResultOption.NegativeDelay, typeof(ArgumentOutOfRangeException))]
    [RequirementCoverage("REQ-VSB-COURIER-RESULT-OPTIONS", "invalid-callbacks-and-delay")]
    public async Task InvalidResultOptions_FaultTheActivityWithTheExactContractExceptionAsync(
        InvalidResultOption option,
        Type expectedExceptionType)
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-invalid-result-options");
        ExecuteActivityTestHarness<InvalidResultOptionsCourierActivity, InvalidResultOptionsArguments> activity = harness.ExecuteActivity<
            InvalidResultOptionsCourierActivity,
            InvalidResultOptionsArguments>();
        using var activityFaulted = new CourierMessageRecorder<RoutingSlipActivityFaulted>(1);
        activityFaulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new InvalidResultOptionsArguments(option));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await activityFaulted.WaitAsync(timeout, cancellationToken);

            ExceptionInfo exception = Assert.Single(activityFaulted.Messages).Message.ExceptionInfo;
            Assert.Equal(TypeCache.GetShortName(expectedExceptionType), exception.ExceptionType);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "execute-activity-owned-cancellation")]
    public async Task ExecuteActivityCancellation_FaultsDeliveryWithoutConvertingItToARoutingSlipFailureAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-execute-cancellation");
        ExecuteActivityTestHarness<CancellingExecuteCourierActivity, CourierArguments> cancelling = harness.ExecuteActivity<
            CancellingExecuteCourierActivity,
            CourierArguments>();
        await harness.StartAsync(cancellationToken);

        try
        {
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddActivity(cancelling.Name, cancelling.ExecuteAddress, new CourierArguments("cancel"));
            Task<IPublishedMessage<Fault<RoutingSlip>>> faultTask = harness.Published
                .SelectAsync<Fault<RoutingSlip>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: cancellationToken);

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            Fault<RoutingSlip> fault = (await faultTask.WaitAsync(timeout, cancellationToken)).Context.Message;

            ExceptionInfo exception = Assert.Single(fault.Exceptions);
            Assert.Equal(TypeCache<ConsumerCanceledException>.ShortName, exception.ExceptionType);
            Assert.Equal(TypeCache<OperationCanceledException>.ShortName, exception.InnerException!.ExceptionType);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "compensate-activity-owned-cancellation")]
    public async Task CompensateActivityCancellation_FaultsDeliveryWithoutPublishingACompletedSlipFailureAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-compensate-cancellation");
        ActivityTestHarness<CancellingCompensationCourierActivity, CourierArguments, CourierLog> cancelling = harness.Activity<
            CancellingCompensationCourierActivity,
            CourierArguments,
            CourierLog>(_ => new CancellingCompensationCourierActivity(), _ => new CancellingCompensationCourierActivity());
        ExecuteActivityTestHarness<FaultingCourierActivity, FaultingCourierArguments> failing = harness.ExecuteActivity<
            FaultingCourierActivity,
            FaultingCourierArguments>();
        await harness.StartAsync(cancellationToken);

        try
        {
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddActivity(cancelling.Name, cancelling.ExecuteAddress, new CourierArguments("cancel"));
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("begin compensation"));
            Task<IPublishedMessage<Fault<RoutingSlip>>> faultTask = harness.Published
                .SelectAsync<Fault<RoutingSlip>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: cancellationToken);

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            Fault<RoutingSlip> fault = (await faultTask.WaitAsync(timeout, cancellationToken)).Context.Message;

            ExceptionInfo exception = Assert.Single(fault.Exceptions);
            Assert.Equal(TypeCache<ConsumerCanceledException>.ShortName, exception.ExceptionType);
            Assert.Equal(TypeCache<OperationCanceledException>.ShortName, exception.InnerException!.ExceptionType);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

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
            Assert.False(slipFailure.Message.Variables.ContainsKey("ActivityValue"));
            ExceptionInfo actual = Assert.Single(slipFailure.Message.ActivityExceptions).ExceptionInfo;
            Assert.Equal(TypeCache<CourierExpectedException>.ShortName, actual.ExceptionType);
            Assert.Equal("thrown-courier-failure", actual.Message);
            Assert.Empty(harness.Published.Select<Fault<RoutingSlip>>(SnapshotOnlyToken()));
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
            Assert.Empty(harness.Published.Select<Fault<RoutingSlip>>(SnapshotOnlyToken()));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-VARIABLES", "compensation-null-removes-existing-variable")]
    public async Task CompensationNullVariable_RemovesTheExistingRoutingSlipVariableAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-compensation-null-variable");
        ActivityTestHarness<FirstCourierActivity, CourierArguments, CourierLog> first = harness.Activity<
            FirstCourierActivity,
            CourierArguments,
            CourierLog>(_ => new FirstCourierActivity(), _ => new FirstCourierActivity());
        ActivityTestHarness<SecondCourierActivity, CourierArguments, CourierLog> second = harness.Activity<
            SecondCourierActivity,
            CourierArguments,
            CourierLog>(_ => new SecondCourierActivity(), _ => new SecondCourierActivity());
        ExecuteActivityTestHarness<FaultingCourierActivity, FaultingCourierArguments> failing = harness.ExecuteActivity<
            FaultingCourierActivity,
            FaultingCourierArguments>();
        using var compensated = new CourierMessageRecorder<RoutingSlipActivityCompensated>(2);
        using var faulted = new CourierMessageRecorder<RoutingSlipFaulted>(1);
        compensated.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddActivity(first.Name, first.ExecuteAddress, new CourierArguments("first"));
            builder.AddActivity(second.Name, second.ExecuteAddress, new CourierArguments("second"));
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("trigger compensation"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                compensated.WaitAsync(timeout, cancellationToken),
                faulted.WaitAsync(timeout, cancellationToken));

            ConsumeContext<RoutingSlipFaulted> slipFailure = Assert.Single(faulted.Messages);
            Assert.False(slipFailure.Message.Variables.ContainsKey("ActivityValue"));
            Assert.Equal(2, compensated.Count);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-FAULT", "handled-domain-fault-does-not-emit-transport-fault")]
    public async Task HandledActivityFault_DoesNotPublishATransportFaultForTheRoutingSlipAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-handled-domain-fault");
        ExecuteActivityTestHarness<FaultingCourierActivity, FaultingCourierArguments> failing = harness.ExecuteActivity<
            FaultingCourierActivity,
            FaultingCourierArguments>();
        using var activityFaulted = new CourierMessageRecorder<RoutingSlipActivityFaulted>(1);
        using var slipFaulted = new CourierMessageRecorder<RoutingSlipFaulted>(1);
        activityFaulted.Configure(harness);
        slipFaulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("handled domain fault"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                activityFaulted.WaitAsync(timeout, cancellationToken),
                slipFaulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Single(activityFaulted.Messages);
            Assert.Single(slipFaulted.Messages);
            Assert.Empty(harness.Published.Select<Fault<RoutingSlip>>(SnapshotOnlyToken()));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);
}
