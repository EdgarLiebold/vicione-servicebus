using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;
using static ViciOne.ServiceBus.Tests.Courier.CourierOutboxJourneyIntegrationTests;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierTimeoutOutboxJourneyIntegrationTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ACTIVITY-TIMEOUT", "courier-outbox-timeout-isolates-effect-and-slip-owner")]
    public async Task ActivityTimeout_DiscardsBufferedEffectsAndKeepsAnIndependentSlipOperationalAsync(
        bool compensate, bool concurrentDelivery)
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        TimeSpan activityTimeout = TimeSpan.FromMinutes(3);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var clock = new FakeTimeProvider(new DateTimeOffset(2040, 3, 4, 5, 6, 7, TimeSpan.Zero));
        var state = new TimeoutState();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-timeout-outbox-journey");
        var activity = harness.AddActivity<DeadlineActivity, JourneyArguments, JourneyLog>(
            _ => new DeadlineActivity(state, harness.InputQueueAddress, compensate),
            _ => new DeadlineActivity(state, harness.InputQueueAddress, compensate));
        activity.ExecuteReceiveEndpointConfiguring += ConfigureEndpoint;
        activity.CompensateReceiveEndpointConfiguring += ConfigureEndpoint;
        var failing = harness.AddExecuteActivity<FaultingCourierActivity, FaultingCourierArguments>();
        using var effects = new CourierMessageRecorder<AttemptEffect>(1);
        using var completed = new CourierMessageRecorder<IRoutingSlipCompleted>(1);
        using var faulted = new CourierMessageRecorder<Fault<IRoutingSlip>>(1);
        using var compensated = new CourierMessageRecorder<IRoutingSlipActivityCompensated>(1);
        using var slipFaulted = new CourierMessageRecorder<IRoutingSlipFaulted>(1);
        using var compensationFailed = new CourierMessageRecorder<IRoutingSlipCompensationFailed>(1);
        using var activityCompensationFailed = new CourierMessageRecorder<IRoutingSlipActivityCompensationFailed>(1);
        effects.Configure(harness);
        completed.Configure(harness);
        faulted.Configure(harness);
        compensated.Configure(harness);
        slipFaulted.Configure(harness);
        compensationFailed.Configure(harness);
        activityCompensationFailed.Configure(harness);
        await harness.StartAsync(cancellationToken);
        using ConnectHandle observer = harness.Bus.ConnectSendObserver(state.Sends);

        try
        {
            Guid pendingId = NewId.NextGuid();
            var pending = new RoutingSlipBuilder(pendingId);
            pending.SetVariable("Owner", "pending-owner");
            pending.AddActivity(activity.Name, activity.ExecuteAddress, new JourneyArguments("pending"));
            if (compensate)
                pending.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("start-compensation"));
            await harness.Bus.ExecuteAsync(pending.Build(), cancellationToken);
            CancellationToken activityToken = await state.Entered.Task.WaitAsync(timeout, cancellationToken);
            Assert.True(activityToken.CanBeCanceled);
            Assert.False(activityToken.IsCancellationRequested);
            Assert.Empty(state.Sends.AdmittedEffects);

            Guid healthyId = NewId.NextGuid();
            var healthy = new RoutingSlipBuilder(healthyId);
            healthy.SetVariable("Owner", "healthy-owner");
            healthy.AddActivity(activity.Name, activity.ExecuteAddress, new JourneyArguments("healthy"));
            await harness.Bus.ExecuteAsync(healthy.Build(), cancellationToken);
            await Task.WhenAll(effects.WaitAsync(timeout, cancellationToken), completed.WaitAsync(timeout, cancellationToken));
            Assert.Equal(healthyId, Assert.Single(completed.Messages).Message.TrackingNumber);
            Assert.Equal("healthy-owner", Assert.Single(completed.Messages).GetVariable<string>("Owner"));
            Assert.Equal(new AttemptEffect(healthyId, "healthy", 0, 0), Assert.Single(effects.Messages).Message);

            clock.Advance(activityTimeout - TimeSpan.FromTicks(1));
            Assert.False(activityToken.IsCancellationRequested);
            Assert.Empty(faulted.Messages);
            Assert.Equal(0, state.Continued);
            clock.Advance(TimeSpan.FromTicks(1));
            Assert.True(activityToken.IsCancellationRequested);

            await faulted.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(cancellationToken);

            ConsumeContext<Fault<IRoutingSlip>> failure = Assert.Single(faulted.Messages);
            Assert.Equal(pendingId, failure.Message.Message.TrackingNumber);
            ExceptionInfo outer = Assert.Single(failure.Message.Exceptions);
            Assert.Equal(TypeCache<ConsumerCanceledException>.ShortName, outer.ExceptionType);
            Assert.Contains(nameof(DeadlineActivity), outer.Message, StringComparison.Ordinal);
            ExceptionInfo deadline = Assert.IsAssignableFrom<ExceptionInfo>(outer.InnerException);
            Assert.Equal(TypeCache<ConsumerCanceledException>.ShortName, deadline.ExceptionType);
            Assert.Contains(activityTimeout.ToString(), deadline.Message, StringComparison.Ordinal);
            Assert.Equal(TypeCache<TaskCanceledException>.ShortName, deadline.InnerException!.ExceptionType);
            Assert.Equal(0, state.Continued);
            Assert.Equal([new AttemptEffect(healthyId, "healthy", 0, 0)], state.Sends.AdmittedEffects);
            Assert.Equal(healthyId, Assert.Single(effects.Messages).Message.TrackingNumber);
            Assert.Equal(healthyId, Assert.Single(completed.Messages).Message.TrackingNumber);
            Assert.Empty(compensated.Messages);
            Assert.Empty(slipFaulted.Messages);
            Assert.Empty(compensationFailed.Messages);
            Assert.Empty(activityCompensationFailed.Messages);
        }
        finally
        {
            state.Release.TrySetResult();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        void ConfigureEndpoint(IReceiveEndpointConfigurator endpoint)
        {
            endpoint.UseTimeout(options =>
            {
                options.Timeout = activityTimeout;
                options.TimeProvider = clock;
            });
            endpoint.UseVolatileOutbox(options => options.ConcurrentMessageDelivery = concurrentDelivery);
        }
    }

    public sealed class TimeoutState
    {
        public JourneyState Sends { get; } = new();
        public TaskCompletionSource<CancellationToken> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Continued;
    }

    public sealed class DeadlineActivity(TimeoutState state, Uri effectAddress, bool compensate)
        : IActivity<JourneyArguments, JourneyLog>
    {
        public async Task<ExecutionResult> ExecuteAsync(ExecuteContext<JourneyArguments> context)
        {
            if (context.Arguments.Value == "healthy")
            {
                ISendEndpoint endpoint = await context.GetSendEndpointAsync(effectAddress, context.CancellationToken);
                await endpoint.SendAsync(new AttemptEffect(context.TrackingNumber, "healthy", 0, 0), context.CancellationToken);
                return context.Completed();
            }

            if (!compensate)
                await WaitForDeadlineAsync(context, "execute-timeout");

            return context.Completed(new JourneyLog(context.Arguments.Value, "resource"));
        }

        public async Task<CompensationResult> CompensateAsync(CompensateContext<JourneyLog> context)
        {
            Assert.Equal(new JourneyLog("pending", "resource"), context.Log);
            await WaitForDeadlineAsync(context, "compensate-timeout");
            return context.Compensated();
        }

        private async Task WaitForDeadlineAsync(ActivityContext context, string phase)
        {
            ISendEndpoint endpoint = await context.GetSendEndpointAsync(effectAddress, context.CancellationToken);
            await endpoint.SendAsync(new AttemptEffect(context.TrackingNumber, phase, 0, 0), context.CancellationToken);
            state.Entered.TrySetResult(context.CancellationToken);
            await state.Release.Task.WaitAsync(context.CancellationToken);
            Interlocked.Increment(ref state.Continued);
        }
    }
}
