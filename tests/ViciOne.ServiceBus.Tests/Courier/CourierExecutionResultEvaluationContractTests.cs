using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Courier.Results;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierExecutionResultEvaluationContractTests
{
    private static readonly DateTimeOffset CreatedAt = new(2048, 9, 10, 11, 12, 13, TimeSpan.Zero);
    private static readonly TimeSpan ActivityDuration = TimeSpan.FromSeconds(7);
    private static readonly Uri CurrentAddress = new("loopback://localhost/result-evaluation-current");
    private static readonly Uri NextAddress = new("loopback://localhost/result-evaluation-next");
    private static readonly Uri ReplacementAddress = new("loopback://localhost/result-evaluation-replacement");
    private static readonly Uri CompensateAddress = new("loopback://localhost/result-evaluation-compensate");

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "execution-result-option-boundaries-and-fault-classification")]
    public void ExecutionResultOptions_ValidateBoundariesAndReportExactFaultState()
    {
        ExecuteFixture fixture = CreateFixture();
        var completed = new CompletedExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            CompensateAddress);
        var expectedFailure = new InvalidOperationException("expected");
        var faulted = new FaultedExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            expectedFailure);

        Assert.IsAssignableFrom<CompletedActivityOptions>(completed);
        Assert.IsAssignableFrom<FaultedActivityOptions>(faulted);
        Assert.False(completed.IsFaulted(out Exception? completedException));
        Assert.Null(completedException);
        Assert.True(faulted.IsFaulted(out Exception? actualFailure));
        Assert.Same(expectedFailure, actualFailure);

        completed.Delay = null;
        Assert.Null(completed.Delay);
        completed.Delay = TimeSpan.Zero;
        Assert.Equal(TimeSpan.Zero, completed.Delay);
        faulted.Delay = TimeSpan.FromTicks(1);
        Assert.Equal(TimeSpan.FromTicks(1), faulted.Delay);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() =>
            completed.Delay = TimeSpan.FromTicks(-1)).ParamName);

        AssertParameter("variables", () => completed.SetVariables((object)null!));
        AssertParameter("variables", () => completed.SetVariables(
            (IEnumerable<KeyValuePair<string, object>>)null!));
        AssertParameter("key", () => completed.SetVariable(" ", "value"));
        AssertParameter("log", () => completed.SetLog<ActivityLog>(null!));
        AssertParameter("values", () => completed.SetLog((object)null!));
        AssertParameter("values", () => completed.SetLog(
            (IEnumerable<KeyValuePair<string, object>>)null!));
        AssertParameter("exception", () => new FaultedExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            null!));
        AssertParameter("itineraryBuilder", () => new ReviseItineraryExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            CompensateAddress,
            null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "completed-terminal-evaluation-publishes-ordered-final-state")]
    public async Task CompletedTerminalEvaluation_PublishesActivityThenRoutingSlipWithExactFinalStateAsync()
    {
        ExecuteFixture fixture = CreateFixture(existingVariable: true);
        var result = new CompletedExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            CompensateAddress);
        result.SetVariable("Output", "completed");
        result.SetVariable("Existing", null);
        result.SetLog(new ActivityLog("receipt"));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await result.EvaluateAsync(cancellationToken);

        AssertCallOrder(fixture.Publisher, "activity-completed", "routing-completed");
        ActivityCompletedObservation activity = Assert.IsType<ActivityCompletedObservation>(fixture.Publisher.ActivityCompleted);
        Assert.Equal("Current", activity.ActivityName);
        Assert.Equal(((ActivityContext)fixture.Context).ExecutionId, activity.ExecutionId);
        Assert.Equal(CreatedAt, activity.Timestamp);
        Assert.Equal(ActivityDuration, activity.Duration);
        Assert.Equal("completed", activity.Variables["output"]);
        Assert.DoesNotContain("existing", activity.Variables);
        Assert.Equal("input", activity.Arguments["value"].ToString());
        Assert.Equal("receipt", activity.Data["value"].ToString());
        Assert.Equal(cancellationToken, activity.CancellationToken);

        RoutingCompletedObservation terminal = Assert.IsType<RoutingCompletedObservation>(fixture.Publisher.RoutingCompleted);
        Assert.Equal(CreatedAt + ActivityDuration, terminal.Timestamp);
        Assert.Equal(ActivityDuration, terminal.Duration);
        Assert.Equal("completed", terminal.Variables["output"]);
        Assert.DoesNotContain("existing", terminal.Variables);
        Assert.Equal(cancellationToken, terminal.CancellationToken);
        Assert.Empty(fixture.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "completed-forward-evaluation-advances-itinerary-once")]
    public async Task CompletedForwardEvaluation_PublishesActivityAndAdvancesToTheNextItineraryEntryAsync()
    {
        ExecuteFixture fixture = CreateFixture(addNextActivity: true);
        var result = new CompletedExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            CompensateAddress);
        result.SetLog(new ActivityLog("forwarded-receipt"));

        await result.EvaluateAsync(TestContext.Current.CancellationToken);

        AssertCallOrder(fixture.Publisher, "activity-completed");
        Assert.Null(fixture.Publisher.RoutingCompleted);
        IRoutingSlip forwarded = Assert.Single(fixture.Outgoing.Messages.OfType<IRoutingSlip>());
        Assert.Equal("Next", Assert.Single(forwarded.Itinerary).Name);
        Assert.Single(forwarded.ActivityLogs);
        ICompensateLog compensateLog = Assert.Single(forwarded.CompensateLogs);
        Assert.Equal(((ActivityContext)fixture.Context).ExecutionId, compensateLog.ExecutionId);
        Assert.Equal(CompensateAddress, compensateLog.Address);
        Assert.Equal("forwarded-receipt", compensateLog.Data["value"].ToString());
        Assert.Equal(2, fixture.RoutingSlip.Itinerary.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "zero-delay-schedules-next-activity-with-exact-token")]
    public async Task ZeroDelay_SchedulesTheNextActivityAtTheContextClockAndForwardsCancellationAsync()
    {
        IAdvancedMessageScheduler scheduler = DispatchProxy.Create<IAdvancedMessageScheduler, RecordingSchedulerProxy>();
        var schedulerProxy = (RecordingSchedulerProxy)(object)scheduler;
        ExecuteFixture fixture = CreateFixture(addNextActivity: true, scheduler: scheduler);
        var result = new CompletedExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            compensationAddress: null)
        {
            Delay = TimeSpan.Zero,
        };
        using var cancellation = new CancellationTokenSource();

        await result.EvaluateAsync(cancellation.Token);

        AssertCallOrder(fixture.Publisher, "activity-completed");
        Assert.Equal(1, schedulerProxy.CallCount);
        Assert.Equal(NextAddress, schedulerProxy.Destination);
        Assert.Equal(fixture.Clock.GetUtcNow(), schedulerProxy.DueAt);
        Assert.Equal(cancellation.Token, schedulerProxy.CancellationToken);
        IRoutingSlip scheduled = Assert.IsAssignableFrom<IRoutingSlip>(schedulerProxy.Message);
        Assert.Equal("Next", Assert.Single(scheduled.Itinerary).Name);
        Assert.Empty(fixture.Outgoing.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "faulted-evaluation-selects-terminal-or-compensation-path")]
    public async Task FaultedEvaluation_PublishesTerminalFaultOrDispatchesCompensationAsync(bool hasCompensation)
    {
        ExecuteFixture fixture = CreateFixture(addCompensation: hasCompensation);
        var expectedFailure = new InvalidOperationException("activity failed");
        var result = new FaultedExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            expectedFailure);
        result.SetVariable("FailureState", "recorded");

        await result.EvaluateAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsFaulted(out Exception? actualFailure));
        Assert.Same(expectedFailure, actualFailure);
        ActivityFaultedObservation activity = Assert.IsType<ActivityFaultedObservation>(fixture.Publisher.ActivityFaulted);
        Assert.Equal("recorded", activity.Variables["failurestate"]);
        Assert.Equal("activity failed", activity.ExceptionInfo.Message);

        if (hasCompensation)
        {
            AssertCallOrder(fixture.Publisher, "activity-faulted");
            Assert.Null(fixture.Publisher.RoutingFaulted);
            IRoutingSlip forwarded = Assert.Single(fixture.Outgoing.Messages.OfType<IRoutingSlip>());
            Assert.Single(forwarded.CompensateLogs);
            IActivityException activityException = Assert.Single(forwarded.ActivityExceptions);
            Assert.Equal(((ActivityContext)fixture.Context).ExecutionId, activityException.ExecutionId);
        }
        else
        {
            AssertCallOrder(fixture.Publisher, "activity-faulted", "routing-faulted");
            RoutingFaultedObservation terminal = Assert.IsType<RoutingFaultedObservation>(fixture.Publisher.RoutingFaulted);
            Assert.Equal(CreatedAt + ActivityDuration, terminal.Timestamp);
            Assert.Equal(ActivityDuration, terminal.Duration);
            Assert.Equal("recorded", terminal.Variables["failurestate"]);
            Assert.Equal(((ActivityContext)fixture.Context).ExecutionId, Assert.Single(terminal.Exceptions).ExecutionId);
            Assert.Empty(fixture.Outgoing.Messages);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "revised-evaluation-publishes-replacement-and-discarded-itineraries")]
    public async Task RevisedEvaluation_PublishesReplacementAndDiscardedItinerariesBeforeForwardingAsync()
    {
        ExecuteFixture fixture = CreateFixture(addNextActivity: true);
        int callbackCount = 0;
        fixture.Publisher.AfterActivityCompleted = () => fixture.Clock.Advance(TimeSpan.FromSeconds(5));
        var result = new ReviseItineraryExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            compensationAddress: null,
            itinerary =>
            {
                callbackCount++;
                itinerary.AddActivity("Replacement", ReplacementAddress, new ActivityArguments("replacement"));
            });

        Assert.Equal(0, callbackCount);
        await result.EvaluateAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, callbackCount);
        AssertCallOrder(fixture.Publisher, "activity-completed", "routing-revised");
        RoutingRevisedObservation revised = Assert.IsType<RoutingRevisedObservation>(fixture.Publisher.RoutingRevised);
        Assert.Equal(CreatedAt, revised.Timestamp);
        Assert.Equal(ActivityDuration, revised.Duration);
        Assert.Equal("Replacement", Assert.Single(revised.Itinerary).Name);
        Assert.Equal("Next", Assert.Single(revised.PreviousItinerary).Name);
        IRoutingSlip forwarded = Assert.Single(fixture.Outgoing.Messages.OfType<IRoutingSlip>());
        Assert.Equal("Replacement", Assert.Single(forwarded.Itinerary).Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "terminated-evaluation-publishes-discarded-and-terminal-state")]
    public async Task TerminatedEvaluation_PublishesDiscardedItineraryThenCompletesWithoutForwardingAsync()
    {
        ExecuteFixture fixture = CreateFixture(addNextActivity: true);
        fixture.Publisher.AfterActivityCompleted = () => fixture.Clock.Advance(TimeSpan.FromSeconds(5));
        var result = new TerminateExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            compensationAddress: null);

        await result.EvaluateAsync(TestContext.Current.CancellationToken);

        AssertCallOrder(fixture.Publisher, "activity-completed", "routing-terminated", "routing-completed");
        RoutingTerminatedObservation terminated = Assert.IsType<RoutingTerminatedObservation>(fixture.Publisher.RoutingTerminated);
        Assert.Equal(CreatedAt, terminated.Timestamp);
        Assert.Equal(ActivityDuration, terminated.Duration);
        Assert.Equal("Next", Assert.Single(terminated.PreviousItinerary).Name);
        Assert.NotNull(fixture.Publisher.RoutingCompleted);
        Assert.Empty(fixture.Outgoing.Messages);
    }

    [Theory]
    [InlineData(ResultShape.Completed)]
    [InlineData(ResultShape.Faulted)]
    [InlineData(ResultShape.Revised)]
    [InlineData(ResultShape.Terminated)]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "activity-publication-failure-short-circuits-every-execution-result")]
    public async Task ActivityPublicationFailure_PropagatesAndPreventsEveryLaterTransitionAsync(ResultShape shape)
    {
        ExecuteFixture fixture = CreateFixture(addNextActivity: true, addCompensation: true);
        var expectedFailure = new InvalidOperationException("publication failed");
        fixture.Publisher.Failure = expectedFailure;
        ExecutionResult result = CreateResult(shape, fixture);

        InvalidOperationException actualFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            result.EvaluateAsync(TestContext.Current.CancellationToken));

        Assert.Same(expectedFailure, actualFailure);
        AssertCallOrder(
            fixture.Publisher,
            shape == ResultShape.Faulted ? "activity-faulted" : "activity-completed");
        Assert.Empty(fixture.Outgoing.Messages);
    }

    [Theory]
    [InlineData(ResultShape.Completed)]
    [InlineData(ResultShape.Faulted)]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "evaluation-cancellation-stops-after-first-lifecycle-publication")]
    public async Task EvaluationCancellation_IsForwardedAndStopsAfterTheFirstLifecyclePublicationAsync(ResultShape shape)
    {
        ExecuteFixture fixture = CreateFixture(addNextActivity: true, addCompensation: true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        ExecutionResult result = CreateResult(shape, fixture);

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            result.EvaluateAsync(cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        PublisherCall call = Assert.Single(fixture.Publisher.Calls);
        Assert.Equal(shape == ResultShape.Faulted ? "activity-faulted" : "activity-completed", call.Name);
        Assert.Equal(cancellation.Token, call.CancellationToken);
        Assert.Empty(fixture.Outgoing.Messages);
    }

    private static ExecutionResult CreateResult(ResultShape shape, ExecuteFixture fixture) => shape switch
    {
        ResultShape.Completed => new CompletedExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            compensationAddress: null),
        ResultShape.Faulted => new FaultedExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            new InvalidOperationException("activity failed")),
        ResultShape.Revised => new ReviseItineraryExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            compensationAddress: null,
            itinerary => itinerary.AddActivity("Replacement", ReplacementAddress, new ActivityArguments("replacement"))),
        ResultShape.Terminated => new TerminateExecutionResult<ActivityArguments>(
            fixture.Context,
            fixture.Publisher,
            fixture.Activity,
            fixture.RoutingSlip,
            compensationAddress: null),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    private static ExecuteFixture CreateFixture(
        bool existingVariable = false,
        bool addNextActivity = false,
        bool addCompensation = false,
        IMessageScheduler? scheduler = null)
    {
        var clock = new FakeTimeProvider(CreatedAt);
        var builder = new RoutingSlipBuilder(
            Guid.Parse("4ac3f875-57c8-4018-9617-b8299745dc04"),
            clock);
        if (existingVariable)
            builder.SetVariable("Existing", "source");

        builder.AddActivity("Current", CurrentAddress, new ActivityArguments("input"));
        if (addNextActivity)
            builder.AddActivity("Next", NextAddress, new ActivityArguments("next"));
        if (addCompensation)
        {
            Guid priorExecutionId = Guid.Parse("53ff0ff5-c076-4468-85f7-bf40c29404f5");
            builder.AddActivityLog(HostMetadataCache.Host, "Prior", priorExecutionId, CreatedAt, TimeSpan.Zero);
            builder.AddCompensateLog(
                priorExecutionId,
                CompensateAddress,
                new Dictionary<string, object> { ["receipt"] = "prior" });
        }

        IRoutingSlip routingSlip = builder.Build();
        var outgoing = new OutgoingMessageRecorder();
        ConsumeContext<IRoutingSlip> consumeContext = CreateConsumeContext(
            routingSlip,
            outgoing,
            clock,
            scheduler);
        var context = new HostExecuteContext<ActivityArguments>(CompensateAddress, consumeContext);
        var publisher = new RecordingRoutingSlipEventPublisher();
        clock.Advance(ActivityDuration);
        return new ExecuteFixture(
            context,
            publisher,
            routingSlip,
            routingSlip.Itinerary[0],
            outgoing,
            clock);
    }

    private static ConsumeContext<IRoutingSlip> CreateConsumeContext(
        IRoutingSlip routingSlip,
        OutgoingMessageRecorder outgoing,
        FakeTimeProvider clock,
        IMessageScheduler? scheduler)
    {
        IObjectDeserializer deserializer = ServiceBusMetadataJson.ObjectDeserializer;
        var metadata = new EnvelopeMessageContext(new JsonMessageEnvelope(), deserializer);
        var serializerContext = new SystemTextJsonSerializerContext(
            deserializer,
            ServiceBusMetadataJson.Options,
            SystemTextJsonMessageSerializer.JsonContentType,
            metadata,
            [MessageUrn.ForTypeString<IRoutingSlip>()],
            message: routingSlip);
        ConsumeContext<IRoutingSlip> context = InMemoryOutboxTestContextFactory.Create(
            routingSlip,
            TestContext.Current.CancellationToken,
            scheduler,
            outgoing,
            serializerContext: serializerContext);
        context.SetTimeProvider(clock);
        return context;
    }

    private static void AssertCallOrder(RecordingRoutingSlipEventPublisher publisher, params string[] expected) =>
        Assert.Equal(expected, publisher.Calls.Select(call => call.Name).ToArray());

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.ThrowsAny<ArgumentException>(action).ParamName);

    public enum ResultShape
    {
        Completed,
        Faulted,
        Revised,
        Terminated,
    }

    private sealed record ActivityArguments(string Value);

    private sealed record ActivityLog(string Value);

    private sealed record ExecuteFixture(
        HostExecuteContext<ActivityArguments> Context,
        RecordingRoutingSlipEventPublisher Publisher,
        IRoutingSlip RoutingSlip,
        IActivity Activity,
        OutgoingMessageRecorder Outgoing,
        FakeTimeProvider Clock);

    private sealed record PublisherCall(string Name, CancellationToken CancellationToken);

    private sealed record ActivityCompletedObservation(
        string ActivityName,
        Guid ExecutionId,
        DateTimeOffset Timestamp,
        TimeSpan Duration,
        IReadOnlyDictionary<string, object> Variables,
        IReadOnlyDictionary<string, object> Arguments,
        IReadOnlyDictionary<string, object> Data,
        CancellationToken CancellationToken);

    private sealed record ActivityFaultedObservation(
        string ActivityName,
        Guid ExecutionId,
        DateTimeOffset Timestamp,
        TimeSpan Duration,
        ExceptionInfo ExceptionInfo,
        IReadOnlyDictionary<string, object> Variables,
        IReadOnlyDictionary<string, object> Arguments,
        CancellationToken CancellationToken);

    private sealed record RoutingCompletedObservation(
        DateTimeOffset Timestamp,
        TimeSpan Duration,
        IReadOnlyDictionary<string, object> Variables,
        CancellationToken CancellationToken);

    private sealed record RoutingFaultedObservation(
        DateTimeOffset Timestamp,
        TimeSpan Duration,
        IReadOnlyDictionary<string, object> Variables,
        IReadOnlyList<IActivityException> Exceptions,
        CancellationToken CancellationToken);

    private sealed record RoutingRevisedObservation(
        DateTimeOffset Timestamp,
        TimeSpan Duration,
        IReadOnlyList<IActivity> Itinerary,
        IReadOnlyList<IActivity> PreviousItinerary,
        CancellationToken CancellationToken);

    private sealed record RoutingTerminatedObservation(
        DateTimeOffset Timestamp,
        TimeSpan Duration,
        IReadOnlyList<IActivity> PreviousItinerary,
        CancellationToken CancellationToken);

    private sealed class RecordingRoutingSlipEventPublisher : IRoutingSlipEventPublisher
    {
        public ActivityCompletedObservation? ActivityCompleted { get; private set; }

        public ActivityFaultedObservation? ActivityFaulted { get; private set; }

        public RoutingCompletedObservation? RoutingCompleted { get; private set; }

        public RoutingFaultedObservation? RoutingFaulted { get; private set; }

        public RoutingRevisedObservation? RoutingRevised { get; private set; }

        public RoutingTerminatedObservation? RoutingTerminated { get; private set; }

        public List<PublisherCall> Calls { get; } = [];

        public Action? AfterActivityCompleted { get; set; }

        public Exception? Failure { get; set; }

        public Task PublishRoutingSlipCompletedAsync(
            DateTimeOffset timestamp,
            TimeSpan duration,
            IReadOnlyDictionary<string, object> variables,
            CancellationToken cancellationToken = default)
        {
            RoutingCompleted = new RoutingCompletedObservation(timestamp, duration, variables, cancellationToken);
            return CompleteAsync("routing-completed", cancellationToken);
        }

        public Task PublishRoutingSlipFaultedAsync(
            DateTimeOffset timestamp,
            TimeSpan duration,
            IReadOnlyDictionary<string, object> variables,
            IReadOnlyCollection<IActivityException> exceptions,
            CancellationToken cancellationToken = default)
        {
            RoutingFaulted = new RoutingFaultedObservation(
                timestamp,
                duration,
                variables,
                exceptions.ToArray(),
                cancellationToken);
            return CompleteAsync("routing-faulted", cancellationToken);
        }

        public Task PublishRoutingSlipActivityCompletedAsync(
            string activityName,
            Guid executionId,
            DateTimeOffset timestamp,
            TimeSpan duration,
            IReadOnlyDictionary<string, object> variables,
            IReadOnlyDictionary<string, object> arguments,
            IReadOnlyDictionary<string, object> data,
            CancellationToken cancellationToken = default)
        {
            ActivityCompleted = new ActivityCompletedObservation(
                activityName,
                executionId,
                timestamp,
                duration,
                variables,
                arguments,
                data,
                cancellationToken);
            return CompleteAsync("activity-completed", cancellationToken);
        }

        public Task PublishRoutingSlipActivityFaultedAsync(
            string activityName,
            Guid executionId,
            DateTimeOffset timestamp,
            TimeSpan duration,
            ExceptionInfo exceptionInfo,
            IReadOnlyDictionary<string, object> variables,
            IReadOnlyDictionary<string, object> arguments,
            CancellationToken cancellationToken = default)
        {
            ActivityFaulted = new ActivityFaultedObservation(
                activityName,
                executionId,
                timestamp,
                duration,
                exceptionInfo,
                variables,
                arguments,
                cancellationToken);
            return CompleteAsync("activity-faulted", cancellationToken);
        }

        public Task PublishRoutingSlipActivityCompensationFailedAsync(
            string activityName,
            Guid executionId,
            DateTimeOffset timestamp,
            TimeSpan duration,
            DateTimeOffset failureTimestamp,
            TimeSpan routingSlipDuration,
            ExceptionInfo exceptionInfo,
            IReadOnlyDictionary<string, object> variables,
            IReadOnlyDictionary<string, object> data,
            CancellationToken cancellationToken = default) =>
            CompleteAsync("activity-compensation-failed", cancellationToken);

        public Task PublishRoutingSlipActivityCompensatedAsync(
            string activityName,
            Guid executionId,
            DateTimeOffset timestamp,
            TimeSpan duration,
            IReadOnlyDictionary<string, object> variables,
            IReadOnlyDictionary<string, object> data,
            CancellationToken cancellationToken = default) =>
            CompleteAsync("activity-compensated", cancellationToken);

        public Task PublishRoutingSlipRevisedAsync(
            string activityName,
            Guid executionId,
            DateTimeOffset timestamp,
            TimeSpan duration,
            IReadOnlyDictionary<string, object> variables,
            IEnumerable<IActivity> itinerary,
            IEnumerable<IActivity> previousItinerary,
            CancellationToken cancellationToken = default)
        {
            RoutingRevised = new RoutingRevisedObservation(
                timestamp,
                duration,
                itinerary.ToArray(),
                previousItinerary.ToArray(),
                cancellationToken);
            return CompleteAsync("routing-revised", cancellationToken);
        }

        public Task PublishRoutingSlipTerminatedAsync(
            string activityName,
            Guid executionId,
            DateTimeOffset timestamp,
            TimeSpan duration,
            IReadOnlyDictionary<string, object> variables,
            IEnumerable<IActivity> previousItinerary,
            CancellationToken cancellationToken = default)
        {
            RoutingTerminated = new RoutingTerminatedObservation(
                timestamp,
                duration,
                previousItinerary.ToArray(),
                cancellationToken);
            return CompleteAsync("routing-terminated", cancellationToken);
        }

        private Task CompleteAsync(string name, CancellationToken cancellationToken)
        {
            Calls.Add(new PublisherCall(name, cancellationToken));
            if (Failure is not null)
                return Task.FromException(Failure);
            if (name == "activity-completed")
                AfterActivityCompleted?.Invoke();
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
        }
    }

    private class RecordingSchedulerProxy : DispatchProxy
    {
        private static readonly MethodInfo CreateScheduledTaskMethod = typeof(RecordingSchedulerProxy)
            .GetMethod(nameof(CreateScheduledTask), BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The scheduled-task factory is missing.");

        public int CallCount { get; private set; }

        public Uri? Destination { get; private set; }

        public DateTimeOffset DueAt { get; private set; }

        public object? Message { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            MethodInfo method = targetMethod
                ?? throw new InvalidOperationException("The scheduler proxy supplied no method metadata.");
            object?[] arguments = args
                ?? throw new InvalidOperationException("The scheduler proxy supplied no arguments.");

            if (method.Name == "get_TimeProvider")
                return TimeProvider.System;
            if (method.Name != nameof(IAdvancedMessageScheduler.ScheduleSendAsync)
                || !method.IsGenericMethod
                || arguments.Length != 5)
                throw new NotSupportedException(method.ToString());

            CallCount++;
            Destination = (Uri)arguments[0]!;
            DueAt = (DateTimeOffset)arguments[1]!;
            Message = arguments[2];
            CancellationToken = (CancellationToken)arguments[4]!;
            return CreateScheduledTaskMethod
                .MakeGenericMethod(method.GetGenericArguments()[0])
                .Invoke(null, [Destination, DueAt, Message, CancellationToken]);
        }

        private static Task<ScheduledMessage<T>> CreateScheduledTask<T>(
            Uri destination,
            DateTimeOffset dueAt,
            object message,
            CancellationToken cancellationToken)
            where T : class
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<ScheduledMessage<T>>(cancellationToken);

            ScheduledMessage<T> scheduled = new ScheduledMessageHandle<T>(
                Guid.Parse("11525de4-e1a5-47bb-801d-dc8406137ec3"),
                dueAt,
                destination,
                (T)message);
            return Task.FromResult(scheduled);
        }
    }
}
