using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipLifecycleIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-LIFECYCLE", "empty-itinerary-completes")]
    public async Task EmptyItinerary_PublishesExactlyOneCompletion()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-empty");
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        completed.Configure(harness);
        await harness.Start(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();

            await harness.Bus.Execute(new RoutingSlipBuilder(trackingNumber).Build(), cancellationToken);
            await completed.Wait(timeout, cancellationToken);
            await harness.Stop();

            Assert.Equal(trackingNumber, Assert.Single(completed.Messages).Message.TrackingNumber);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-LIFECYCLE", "single-activity-complete-event-shape")]
    public async Task SingleActivity_PublishesOneCompleteActivityAndSlipEventWithExactData()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-single");
        ActivityTestHarness<FirstCourierActivity, CourierArguments, CourierLog> activity = harness.Activity<
            FirstCourierActivity,
            CourierArguments,
            CourierLog>(_ => new FirstCourierActivity(), _ => new FirstCourierActivity());
        using var activityCompleted = new CourierMessageRecorder<RoutingSlipActivityCompleted>(1);
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        activityCompleted.Configure(harness);
        completed.Configure(harness);
        await harness.Start(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new CourierArguments("original"));
            builder.AddVariable("SlipVariable", "knife");

            await harness.Bus.Execute(builder.Build(), cancellationToken);
            await Task.WhenAll(
                activityCompleted.Wait(timeout, cancellationToken),
                completed.Wait(timeout, cancellationToken));
            await harness.Stop();

            ConsumeContext<RoutingSlipActivityCompleted> activityEvent = Assert.Single(activityCompleted.Messages);
            ConsumeContext<RoutingSlipCompleted> completion = Assert.Single(completed.Messages);
            Assert.Equal(trackingNumber, activityEvent.Message.TrackingNumber);
            Assert.Equal(trackingNumber, completion.Message.TrackingNumber);
            Assert.Equal(activity.Name, activityEvent.Message.ActivityName);
            Assert.Equal("original", activityEvent.GetResult<string>(nameof(CourierLog.OriginalValue)));
            Assert.Equal("knife", activityEvent.GetVariable<string>("SlipVariable"));
            Assert.Equal("knife", completion.GetVariable<string>("SlipVariable"));
            Assert.Equal("first-output", completion.GetVariable<string>("ActivityValue"));
            Assert.Equal(activityEvent.Message.Timestamp + activityEvent.Message.Duration, completion.Message.Timestamp);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-LIFECYCLE", "two-activity-variable-and-log-evolution")]
    public async Task TwoActivities_PreserveEachLogAndApplyVariableUpdatesExactlyOnce()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-two");
        ActivityTestHarness<FirstCourierActivity, CourierArguments, CourierLog> first = harness.Activity<
            FirstCourierActivity,
            CourierArguments,
            CourierLog>(_ => new FirstCourierActivity(), _ => new FirstCourierActivity());
        ActivityTestHarness<SecondCourierActivity, CourierArguments, CourierLog> second = harness.Activity<
            SecondCourierActivity,
            CourierArguments,
            CourierLog>(_ => new SecondCourierActivity(), _ => new SecondCourierActivity());
        using var activityCompleted = new CourierMessageRecorder<RoutingSlipActivityCompleted>(2);
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        activityCompleted.Configure(harness);
        completed.Configure(harness);
        await harness.Start(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(first.Name, first.ExecuteAddress, new CourierArguments("first"));
            builder.AddActivity(second.Name, second.ExecuteAddress, new CourierArguments("second"));
            builder.AddVariable("SlipVariable", "knife");

            await harness.Bus.Execute(builder.Build(), cancellationToken);
            await Task.WhenAll(
                activityCompleted.Wait(timeout, cancellationToken),
                completed.Wait(timeout, cancellationToken));
            await harness.Stop();

            Assert.Equal(2, activityCompleted.Count);
            ConsumeContext<RoutingSlipActivityCompleted> firstEvent = Assert.Single(
                activityCompleted.Messages,
                context => context.Message.ActivityName == first.Name);
            ConsumeContext<RoutingSlipActivityCompleted> secondEvent = Assert.Single(
                activityCompleted.Messages,
                context => context.Message.ActivityName == second.Name);
            Assert.Equal("first", firstEvent.GetResult<string>(nameof(CourierLog.OriginalValue)));
            Assert.Equal("second", secondEvent.GetResult<string>(nameof(CourierLog.OriginalValue)));
            Assert.Equal("knife", firstEvent.GetVariable<string>("SlipVariable"));
            ConsumeContext<RoutingSlipCompleted> completion = Assert.Single(completed.Messages);
            Assert.Equal(trackingNumber, completion.Message.TrackingNumber);
            Assert.Equal("first-output", completion.GetVariable<string>("ActivityValue"));
            Assert.Equal("second-output", completion.GetVariable<string>("SecondValue"));
            Assert.False(completion.Message.Variables.ContainsKey("RemovedBySecond"));
        }
        finally
        {
            await harness.Stop();
        }
    }
}
