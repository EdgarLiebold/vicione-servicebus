using System.Collections.Concurrent;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierOutboxJourneyIntegrationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-COURIER-REVISION", "retry-outbox-commits-only-selected-route-and-attempt")]
    public async Task ExecuteRetry_RevisesTheRouteAndDeliversOnlyCommittedAttemptEffectsAsync(
        bool concurrentDelivery, bool terminate)
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var state = new JourneyState();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-execute-outbox-journey");
        var next = harness.AddExecuteActivity<RecordingActivity, JourneyArguments>(_ => new RecordingActivity(state));
        var executing = harness.AddExecuteActivity<RevisingActivity, JourneyArguments>(
            _ => new RevisingActivity(state, harness.InputQueueAddress, () => next.ExecuteAddress, terminate));
        executing.ExecuteReceiveEndpointConfiguring += endpoint => ConfigureRetryOutbox(endpoint, concurrentDelivery);
        using var effects = new CourierMessageRecorder<AttemptEffect>(2);
        using var completed = new CourierMessageRecorder<IRoutingSlipCompleted>(1);
        using var revised = new CourierMessageRecorder<IRoutingSlipRevised>(1);
        using var terminated = new CourierMessageRecorder<IRoutingSlipTerminated>(1);
        effects.Configure(harness);
        completed.Configure(harness);
        revised.Configure(harness);
        terminated.Configure(harness);
        await harness.StartAsync(cancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectSendObserver(state);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.SetVariable("Seed", "original-seed");
            builder.AddActivity(executing.Name, executing.ExecuteAddress, new JourneyArguments("execute-input"));
            builder.AddActivity("Discarded", next.ExecuteAddress, new JourneyArguments("must-not-run"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await state.Pending.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(
                [new AttemptObservation(trackingNumber, 0, "execute-input", "original-seed"),
                    new AttemptObservation(trackingNumber, 1, "execute-input", "original-seed")],
                state.Attempts);
            Assert.Empty(state.AdmittedEffects);
            Assert.Empty(effects.Messages);
            Assert.Empty(state.Executed);
            Assert.Empty(completed.Messages);
            Assert.Empty(revised.Messages);
            Assert.Empty(terminated.Messages);

            state.Release.TrySetResult();
            await Task.WhenAll(
                effects.WaitAsync(timeout, cancellationToken),
                completed.WaitAsync(timeout, cancellationToken),
                terminate ? terminated.WaitAsync(timeout, cancellationToken) : revised.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(cancellationToken);

            AssertEffects(state, effects, trackingNumber, "execute");
            ConsumeContext<IRoutingSlipCompleted> completion = Assert.Single(completed.Messages);
            Assert.Equal(trackingNumber, completion.Message.TrackingNumber);
            Assert.Equal("original-seed", completion.GetVariable<string>("Seed"));
            Assert.Equal("committed-route", completion.GetVariable<string>("Outcome"));
            if (terminate)
            {
                Assert.Empty(state.Executed);
                Assert.Empty(revised.Messages);
                ConsumeContext<IRoutingSlipTerminated> terminal = Assert.Single(terminated.Messages);
                Assert.Equal(trackingNumber, terminal.Message.TrackingNumber);
                Assert.Equal(executing.Name, terminal.Message.ActivityName);
                Assert.Equal("Discarded", Assert.Single(terminal.Message.DiscardedItinerary).Name);
            }
            else
            {
                Assert.Equal(["replacement:committed-route"], state.Executed);
                Assert.Empty(terminated.Messages);
                ConsumeContext<IRoutingSlipRevised> revision = Assert.Single(revised.Messages);
                Assert.Equal(trackingNumber, revision.Message.TrackingNumber);
                IActivity replacement = Assert.Single(revision.Message.Itinerary);
                Assert.Equal("Replacement", replacement.Name);
                Assert.Equal(next.ExecuteAddress, replacement.Address);
                Assert.Equal("Discarded", Assert.Single(revision.Message.DiscardedItinerary).Name);
            }
            Assert.Equal(2, state.Attempts.Count);
        }
        finally
        {
            state.Release.TrySetResult();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-COURIER-RETRY", "compensation-outbox-discard-preserves-log-and-variable-removal")]
    public async Task CompensationRetry_DiscardsFailedEffectsAndPreservesLogAndVariableRemovalAsync(
        bool concurrentDelivery, bool exhaustRetries)
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var state = new JourneyState();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-compensate-outbox-journey");
        var compensating = harness.AddActivity<CompensatingActivity, JourneyArguments, JourneyLog>(
            _ => new CompensatingActivity(state, harness.InputQueueAddress, exhaustRetries),
            _ => new CompensatingActivity(state, harness.InputQueueAddress, exhaustRetries));
        compensating.ExecuteReceiveEndpointConfiguring += endpoint => endpoint.UseVolatileOutbox();
        compensating.CompensateReceiveEndpointConfiguring += endpoint => ConfigureRetryOutbox(endpoint, concurrentDelivery);
        var failing = harness.AddExecuteActivity<FaultingCourierActivity, FaultingCourierArguments>();
        using var effects = new CourierMessageRecorder<AttemptEffect>(2);
        using var compensated = new CourierMessageRecorder<IRoutingSlipActivityCompensated>(1);
        using var faulted = new CourierMessageRecorder<IRoutingSlipFaulted>(1);
        using var failed = new CourierMessageRecorder<IRoutingSlipCompensationFailed>(1);
        using var activityFailed = new CourierMessageRecorder<IRoutingSlipActivityCompensationFailed>(1);
        using var transportFaulted = new CourierMessageRecorder<Fault<IRoutingSlip>>(1);
        effects.Configure(harness);
        compensated.Configure(harness);
        faulted.Configure(harness);
        failed.Configure(harness);
        activityFailed.Configure(harness);
        transportFaulted.Configure(harness);
        await harness.StartAsync(cancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectSendObserver(state);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.SetVariable("Seed", "compensation-seed");
            builder.AddActivity(compensating.Name, compensating.ExecuteAddress, new JourneyArguments("original-log"));
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("downstream-failure"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await state.Pending.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(
                [new AttemptObservation(trackingNumber, 0, "original-log", "compensation-seed"),
                    new AttemptObservation(trackingNumber, 1, "original-log", "compensation-seed")],
                state.Attempts);
            Assert.Empty(state.AdmittedEffects);
            Assert.Empty(effects.Messages);
            Assert.Empty(compensated.Messages);
            Assert.Empty(faulted.Messages);
            Assert.Empty(failed.Messages);

            state.Release.TrySetResult();
            if (exhaustRetries)
                await Task.WhenAll(failed.WaitAsync(timeout, cancellationToken), activityFailed.WaitAsync(timeout, cancellationToken));
            else
                await Task.WhenAll(
                    effects.WaitAsync(timeout, cancellationToken),
                    compensated.WaitAsync(timeout, cancellationToken),
                    faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(cancellationToken);

            if (exhaustRetries)
            {
                Assert.Empty(state.AdmittedEffects);
                Assert.Empty(effects.Messages);
                Assert.Empty(compensated.Messages);
                Assert.Empty(faulted.Messages);
                ConsumeContext<IRoutingSlipCompensationFailed> failure = Assert.Single(failed.Messages);
                Assert.Equal(trackingNumber, failure.Message.TrackingNumber);
                Assert.Equal("compensation-exhausted", failure.Message.ExceptionInfo.Message);
                Assert.Equal(TypeCache<CourierExpectedException>.ShortName, failure.Message.ExceptionInfo.ExceptionType);
                Assert.Equal("reserved-resource", failure.GetVariable<string>("Reservation"));
                Assert.Equal("compensation-seed", failure.GetVariable<string>("Seed"));
                Assert.False(failure.Message.Variables.ContainsKey("Outcome"));
                ConsumeContext<IRoutingSlipActivityCompensationFailed> activityFailure = Assert.Single(activityFailed.Messages);
                Assert.Equal(trackingNumber, activityFailure.Message.TrackingNumber);
                Assert.Equal(compensating.Name, activityFailure.Message.ActivityName);
                Assert.Equal("original-log", activityFailure.GetResult<string>(nameof(JourneyLog.Original)));
                Assert.Equal("reserved-resource", activityFailure.GetResult<string>(nameof(JourneyLog.Resource)));
                Assert.Equal("compensation-exhausted", activityFailure.Message.ExceptionInfo.Message);
            }
            else
            {
                AssertEffects(state, effects, trackingNumber, "compensate");
                ConsumeContext<IRoutingSlipActivityCompensated> compensation = Assert.Single(compensated.Messages);
                Assert.Equal(trackingNumber, compensation.Message.TrackingNumber);
                Assert.Equal(compensating.Name, compensation.Message.ActivityName);
                Assert.Equal("original-log", compensation.GetResult<string>(nameof(JourneyLog.Original)));
                Assert.Equal("reserved-resource", compensation.GetResult<string>(nameof(JourneyLog.Resource)));
                ConsumeContext<IRoutingSlipFaulted> terminal = Assert.Single(faulted.Messages);
                Assert.Equal(trackingNumber, terminal.Message.TrackingNumber);
                Assert.Equal("compensation-seed", terminal.GetVariable<string>("Seed"));
                Assert.Equal("released", terminal.GetVariable<string>("Outcome"));
                Assert.False(terminal.Message.Variables.ContainsKey("Reservation"));
                Assert.Equal("downstream-failure", Assert.Single(terminal.Message.ActivityExceptions).ExceptionInfo.Message);
                Assert.Empty(failed.Messages);
                Assert.Empty(activityFailed.Messages);
            }
            Assert.Empty(transportFaulted.Messages);
            Assert.Equal(2, state.Attempts.Count);
        }
        finally
        {
            state.Release.TrySetResult();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void ConfigureRetryOutbox(IReceiveEndpointConfigurator endpoint, bool concurrentDelivery)
    {
        endpoint.UseMessageRetry(retry => retry.Immediate(1));
        endpoint.UseVolatileOutbox(outbox => outbox.ConcurrentMessageDelivery = concurrentDelivery);
    }

    private static void AssertEffects(
        JourneyState state, CourierMessageRecorder<AttemptEffect> effects, Guid trackingNumber, string phase)
    {
        AttemptEffect[] expected =
        [new(trackingNumber, phase, 1, 0), new(trackingNumber, phase, 1, 1)];
        Assert.Equal(expected, state.AdmittedEffects.OrderBy(effect => effect.Sequence));
        Assert.Equal(expected, effects.Messages.Select(context => context.Message).OrderBy(effect => effect.Sequence));
    }

    public sealed record JourneyArguments(string Value);
    public sealed record JourneyLog(string Original, string Resource);
    public sealed record AttemptEffect(Guid TrackingNumber, string Phase, int Attempt, int Sequence);
    public sealed record AttemptObservation(Guid TrackingNumber, int Attempt, string Value, string Seed);

    public sealed class JourneyState : ISendObserver
    {
        public ConcurrentQueue<AttemptObservation> Attempts { get; } = new();
        public ConcurrentQueue<AttemptEffect> AdmittedEffects { get; } = new();
        public ConcurrentQueue<string> Executed { get; } = new();
        public TaskCompletionSource Pending { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PreSendAsync<T>(SendContext<T> context) where T : class
        {
            if (context.Message is AttemptEffect effect)
                AdmittedEffects.Enqueue(effect);
            return Task.CompletedTask;
        }

        public Task PostSendAsync<T>(SendContext<T> context) where T : class => Task.CompletedTask;
        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }

    public sealed class RevisingActivity(JourneyState state, Uri effectAddress, Func<Uri> replacementAddress, bool terminate)
        : IExecuteActivity<JourneyArguments>
    {
        public async Task<ExecutionResult> ExecuteAsync(ExecuteContext<JourneyArguments> context)
        {
            int attempt = context.GetRetryAttempt();
            state.Attempts.Enqueue(new AttemptObservation(
                context.TrackingNumber, attempt, context.Arguments.Value, context.GetVariable<string>("Seed")!));
            await BufferEffectsAsync(context, effectAddress, "execute", attempt);
            if (attempt == 0)
                throw new CourierExpectedException("discard-execute-attempt");

            state.Pending.TrySetResult();
            await state.Release.Task.WaitAsync(context.CancellationToken);
            if (terminate)
                return context.Terminate(new { Outcome = "committed-route" });

            return context.ReviseItinerary(itinerary =>
            {
                itinerary.SetVariable("Outcome", "committed-route");
                itinerary.AddActivity("Replacement", replacementAddress(), new JourneyArguments("replacement"));
            });
        }
    }

    public sealed class RecordingActivity(JourneyState state) : IExecuteActivity<JourneyArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<JourneyArguments> context)
        {
            state.Executed.Enqueue($"{context.Arguments.Value}:{context.GetVariable<string>("Outcome")}");
            return Task.FromResult(context.Completed());
        }
    }

    public sealed class CompensatingActivity(JourneyState state, Uri effectAddress, bool exhaustRetries)
        : IActivity<JourneyArguments, JourneyLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<JourneyArguments> context) =>
            Task.FromResult(context.CompletedWithVariables(
                new JourneyLog(context.Arguments.Value, "reserved-resource"), new { Reservation = "reserved-resource" }));

        public async Task<CompensationResult> CompensateAsync(CompensateContext<JourneyLog> context)
        {
            int attempt = context.GetRetryAttempt();
            state.Attempts.Enqueue(new AttemptObservation(
                context.TrackingNumber, attempt, context.Log.Original, context.GetVariable<string>("Seed")!));
            Assert.Equal("reserved-resource", context.Log.Resource);
            Assert.Equal("reserved-resource", context.GetVariable<string>("Reservation"));
            await BufferEffectsAsync(context, effectAddress, "compensate", attempt);
            if (attempt == 0)
                throw new CourierExpectedException("discard-compensation-attempt");

            state.Pending.TrySetResult();
            await state.Release.Task.WaitAsync(context.CancellationToken);
            if (exhaustRetries)
                return context.Failed(new CourierExpectedException("compensation-exhausted"));

            return context.Compensated(new Dictionary<string, object>
            {
                ["Reservation"] = null!,
                ["Outcome"] = "released",
            });
        }
    }

    private static async Task BufferEffectsAsync(ActivityContext context, Uri address, string phase, int attempt)
    {
        ISendEndpoint endpoint = await context.GetSendEndpointAsync(address, context.CancellationToken);
        await endpoint.SendAsync(new AttemptEffect(context.TrackingNumber, phase, attempt, 0), context.CancellationToken);
        await endpoint.SendAsync(new AttemptEffect(context.TrackingNumber, phase, attempt, 1), context.CancellationToken);
    }
}
