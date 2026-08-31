using System.Collections.Concurrent;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipRevisionAndSubscriptionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REVISION", "append-and-preserve-source-itinerary")]
    public async Task Revision_AppendsAnActivityThenContinuesWithTheSourceItinerary()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var executionOrder = new ConcurrentQueue<string>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-revision-preserve");
        ExecuteActivityTestHarness<RecordingRevisionActivity, RevisionArguments> recording = harness.ExecuteActivity<
            RecordingRevisionActivity,
            RevisionArguments>(_ => new RecordingRevisionActivity(executionOrder));
        ExecuteActivityTestHarness<RevisingActivity, RevisionArguments> revising = harness.ExecuteActivity<
            RevisingActivity,
            RevisionArguments>(_ => new RevisingActivity(() => recording.ExecuteAddress, preserveSource: true));
        using var revised = new CourierMessageRecorder<RoutingSlipRevised>(1);
        using var activityCompleted = new CourierMessageRecorder<RoutingSlipActivityCompleted>(3);
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        revised.Configure(harness);
        activityCompleted.Configure(harness);
        completed.Configure(harness);
        await harness.Start(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(revising.Name, revising.ExecuteAddress, new RevisionArguments("revise"));
            builder.AddActivity("Source", recording.ExecuteAddress, new RevisionArguments("source"));

            await harness.Bus.Execute(builder.Build(), cancellationToken);
            await Task.WhenAll(
                revised.Wait(timeout, cancellationToken),
                activityCompleted.Wait(timeout, cancellationToken),
                completed.Wait(timeout, cancellationToken));
            await harness.Stop();

            Assert.Equal(["appended", "source"], executionOrder);
            ConsumeContext<RoutingSlipRevised> revision = Assert.Single(revised.Messages);
            Assert.Equal(trackingNumber, revision.Message.TrackingNumber);
            Assert.Equal(["Appended", "Source"], revision.Message.Itinerary.Select(x => x.Name));
            Assert.Empty(revision.Message.DiscardedItinerary);
            Assert.Equal(3, activityCompleted.Count);
            Assert.Single(completed.Messages);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REVISION", "discard-source-and-complete")]
    public async Task Revision_CanDiscardTheEntireSourceItineraryAndCompleteImmediately()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var executionOrder = new ConcurrentQueue<string>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-revision-discard");
        ExecuteActivityTestHarness<RecordingRevisionActivity, RevisionArguments> recording = harness.ExecuteActivity<
            RecordingRevisionActivity,
            RevisionArguments>(_ => new RecordingRevisionActivity(executionOrder));
        ExecuteActivityTestHarness<RevisingActivity, RevisionArguments> revising = harness.ExecuteActivity<
            RevisingActivity,
            RevisionArguments>(_ => new RevisingActivity(() => recording.ExecuteAddress, preserveSource: false));
        using var revised = new CourierMessageRecorder<RoutingSlipRevised>(1);
        using var activityCompleted = new CourierMessageRecorder<RoutingSlipActivityCompleted>(1);
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        revised.Configure(harness);
        activityCompleted.Configure(harness);
        completed.Configure(harness);
        await harness.Start(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(revising.Name, revising.ExecuteAddress, new RevisionArguments("discard"));
            builder.AddActivity("Discarded", recording.ExecuteAddress, new RevisionArguments("must-not-run"));

            await harness.Bus.Execute(builder.Build(), cancellationToken);
            await Task.WhenAll(
                revised.Wait(timeout, cancellationToken),
                activityCompleted.Wait(timeout, cancellationToken),
                completed.Wait(timeout, cancellationToken));
            await harness.Stop();

            Assert.Empty(executionOrder);
            ConsumeContext<RoutingSlipRevised> revision = Assert.Single(revised.Messages);
            Assert.Equal(trackingNumber, revision.Message.TrackingNumber);
            Assert.Empty(revision.Message.Itinerary);
            Activity discarded = Assert.Single(revision.Message.DiscardedItinerary);
            Assert.Equal("Discarded", discarded.Name);
            Assert.Equal(recording.ExecuteAddress, discarded.Address);
            Assert.Single(activityCompleted.Messages);
            Assert.Single(completed.Messages);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "activity-added-subscription")]
    public async Task RevisionAddedSubscription_ReceivesTheAppendedActivityAndTerminalCompletion()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var executionOrder = new ConcurrentQueue<string>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-revision-subscription");
        ExecuteActivityTestHarness<RecordingRevisionActivity, RevisionArguments> recording = harness.ExecuteActivity<
            RecordingRevisionActivity,
            RevisionArguments>(_ => new RecordingRevisionActivity(executionOrder));
        ExecuteActivityTestHarness<SubscriptionRevisingActivity, RevisionArguments> revising = harness.ExecuteActivity<
            SubscriptionRevisingActivity,
            RevisionArguments>(_ => new SubscriptionRevisingActivity(
                () => recording.ExecuteAddress,
                harness.InputQueueAddress));
        using var revised = new CourierMessageRecorder<RoutingSlipRevised>(1);
        using var activityCompleted = new CourierMessageRecorder<RoutingSlipActivityCompleted>(2);
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        revised.Configure(harness);
        activityCompleted.Configure(harness);
        completed.Configure(harness);
        await harness.Start(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(revising.Name, revising.ExecuteAddress, new RevisionArguments("subscribe"));

            await harness.Bus.Execute(builder.Build(), cancellationToken);
            await Task.WhenAll(
                revised.Wait(timeout, cancellationToken),
                activityCompleted.Wait(timeout, cancellationToken),
                completed.Wait(timeout, cancellationToken));
            await harness.Stop();

            Assert.Equal(["subscribed"], executionOrder);
            ConsumeContext<RoutingSlipActivityCompleted> appended = Assert.Single(
                activityCompleted.Messages,
                context => context.Message.ActivityName == "Subscribed");
            Assert.Equal("subscribed", appended.GetArgument<string>(nameof(RevisionArguments.Label)));
            Assert.Equal(trackingNumber, appended.Message.TrackingNumber);
            Assert.Single(revised.Messages);
            Assert.Single(completed.Messages);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "event-contents-none")]
    public async Task SubscriptionWithNoContents_ExcludesVariablesAndActivityDataButKeepsIdentity()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-subscription-none");
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
            builder.AddSubscription(
                harness.InputQueueAddress,
                RoutingSlipEvents.ActivityCompleted | RoutingSlipEvents.Completed,
                RoutingSlipEventContents.None);
            builder.AddVariable("Variable", "knife");
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new CourierArguments("original"));

            await harness.Bus.Execute(builder.Build(), cancellationToken);
            await Task.WhenAll(
                activityCompleted.Wait(timeout, cancellationToken),
                completed.Wait(timeout, cancellationToken));
            await harness.Stop();

            ConsumeContext<RoutingSlipActivityCompleted> actual = Assert.Single(activityCompleted.Messages);
            Assert.Equal(trackingNumber, actual.Message.TrackingNumber);
            Assert.Empty(actual.Message.Data);
            Assert.Empty(actual.Message.Variables);
            Assert.Equal(trackingNumber, Assert.Single(completed.Messages).Message.TrackingNumber);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-SUBSCRIPTION", "custom-completion-message-default-and-raw-json")]
    public async Task CustomCompletionMessage_RoundTripsWithDefaultAndRawJson(bool useRawJson)
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness(
            useRawJson ? "courier-subscription-raw" : "courier-subscription-envelope");
        if (useRawJson)
            harness.OnConfigureInMemoryBus += configurator => configurator.UseRawJsonSerializer();
        ExecuteActivityTestHarness<CustomEventActivity, CustomEventArguments> activity = harness.ExecuteActivity<
            CustomEventActivity,
            CustomEventArguments>();
        using var customCompleted = new CourierMessageRecorder<RegistrationCompleted>(1);
        customCompleted.Configure(harness);
        await harness.Start(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var staleTimestamp = new DateTime(2001, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            var builder = new RoutingSlipBuilder(trackingNumber);
            await builder.AddSubscription(
                harness.InputQueueAddress,
                RoutingSlipEvents.Completed,
                RoutingSlipEventContents.All,
                endpoint => endpoint.Send<RegistrationCompleted>(new
                {
                    TrackingNumber = trackingNumber,
                    Timestamp = staleTimestamp,
                    Value = "Secret Value",
                }, cancellationToken));
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new CustomEventArguments("payload"));

            await harness.Bus.Execute(builder.Build(), cancellationToken);
            await customCompleted.Wait(timeout, cancellationToken);
            await harness.Stop();

            ConsumeContext<RegistrationCompleted> actual = Assert.Single(customCompleted.Messages);
            Assert.Equal(trackingNumber, actual.Message.TrackingNumber);
            Assert.Equal("Secret Value", actual.Message.Value);
            Assert.NotEqual(default, actual.Message.Timestamp);
            Assert.NotEqual(staleTimestamp, actual.Message.Timestamp);
            Assert.True(actual.Message.Duration >= TimeSpan.Zero);
        }
        finally
        {
            await harness.Stop();
        }
    }

    public sealed record RevisionArguments(string Label);

    public sealed class RecordingRevisionActivity(ConcurrentQueue<string> executionOrder) :
        IExecuteActivity<RevisionArguments>
    {
        public Task<ExecutionResult> Execute(ExecuteContext<RevisionArguments> context)
        {
            executionOrder.Enqueue(context.Arguments.Label);
            return Task.FromResult(context.Completed());
        }
    }

    public sealed class RevisingActivity(Func<Uri> recordingAddress, bool preserveSource) :
        IExecuteActivity<RevisionArguments>
    {
        public Task<ExecutionResult> Execute(ExecuteContext<RevisionArguments> context) =>
            Task.FromResult(context.ReviseItinerary(itinerary =>
            {
                if (preserveSource)
                {
                    itinerary.AddActivity("Appended", recordingAddress(), new RevisionArguments("appended"));
                    itinerary.AddActivitiesFromSourceItinerary();
                }
            }));
    }

    public sealed class SubscriptionRevisingActivity(Func<Uri> recordingAddress, Uri subscriptionAddress) :
        IExecuteActivity<RevisionArguments>
    {
        public Task<ExecutionResult> Execute(ExecuteContext<RevisionArguments> context) =>
            Task.FromResult(context.ReviseItinerary(itinerary =>
            {
                itinerary.AddActivity("Subscribed", recordingAddress(), new RevisionArguments("subscribed"));
                itinerary.AddSubscription(
                    subscriptionAddress,
                    RoutingSlipEvents.ActivityCompleted | RoutingSlipEvents.Completed,
                    RoutingSlipEventContents.All,
                    "Subscribed");
            }));
    }

    public sealed record CustomEventArguments(string Value);

    public sealed class CustomEventActivity : IExecuteActivity<CustomEventArguments>
    {
        public Task<ExecutionResult> Execute(ExecuteContext<CustomEventArguments> context) =>
            Task.FromResult(context.Completed());
    }

    public interface RegistrationCompleted : RoutingSlipCompleted
    {
        string Value { get; }
    }
}
