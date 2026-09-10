using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.InMemoryTransport;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineSchedulingIntegrationTests
{
    private static readonly TimeSpan InstanceDelay = TimeSpan.FromHours(4);

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-SCHEDULE", "correlated-instance-delay-and-exact-deadline")]
    public async Task CorrelatedSchedule_UsesTheInstanceDelayAndFinalizesAtTheExactAdvancedDeadlineAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var scheduleObservation = new ScheduleObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(scheduleObservation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.SetEndpointNameFormatter(
                    new KebabCaseEndpointNameFormatter($"schedule-native-{NewId.NextGuid():N}"));
                configuration.AddSagaStateMachine<ScheduledMachine, ScheduledState>();
                configuration.UsingInMemory((context, bus) =>
                {
                    bus.ConfigureDelayedMessageScheduler();
                    bus.UseConsumeFilter(typeof(RecordingSchedulerFilter<>), context);
                    bus.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        ISagaStateMachineTestHarness<ScheduledMachine, ScheduledState> sagaHarness =
            harness.GetSagaStateMachineHarness<ScheduledMachine, ScheduledState>();
        ILoadSagaRepository<ScheduledState> repository = provider.GetRequiredService<ILoadSagaRepository<ScheduledState>>();

        try
        {
            Guid correlationId = NewId.NextGuid();
            Task<IConsumedMessage<TimeoutNotice>> timeoutDelivery = sagaHarness.Consumed
                .SelectAsync<TimeoutNotice>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IPublishedMessage<ScheduleCompleted>> completed = harness.Published
                .SelectAsync<ScheduleCompleted>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.Bus.PublishAsync(new ScheduleStart(correlationId, InstanceDelay), cancellationToken);
            Assert.Equal(correlationId, await sagaHarness.WaitForSagaInStateAsync(correlationId, state => state.Waiting, timeout, TestContext.Current.CancellationToken));
            Assert.Equal(
                await scheduleObservation.Expected.Task.WaitAsync(timeout, cancellationToken),
                await scheduleObservation.Actual.Task.WaitAsync(timeout, cancellationToken));
            Assert.False(timeoutDelivery.IsCompleted);

            IInMemoryDelayProvider delayProvider = provider.GetRequiredService<IInMemoryDelayProvider>();
            delayProvider.Advance(InstanceDelay - TimeSpan.FromTicks(1));
            Assert.False(timeoutDelivery.IsCompleted);

            delayProvider.Advance(TimeSpan.FromTicks(1));
            IConsumedMessage<TimeoutNotice> received = await timeoutDelivery.WaitAsync(timeout, cancellationToken);
            ScheduleCompleted result = (await completed.WaitAsync(timeout, cancellationToken)).Context.Message;

            Assert.Null(received.Exception);
            Assert.Equal(correlationId, received.Context.Message.CorrelationId);
            Assert.Equal(new ScheduleCompleted(correlationId, "expired"), result);
            Assert.Null(await repository.LoadAsync(correlationId, TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(sagaHarness.Consumed.Snapshot<TimeoutNotice>());
        Assert.Single(harness.Published.Snapshot<ScheduleCompleted>());
    }


    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record ScheduleStart(Guid CorrelationId, TimeSpan Delay) : CorrelatedBy<Guid>;

    public sealed record TimeoutNotice(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ScheduleCompleted(Guid CorrelationId, string Result);

    public sealed class ScheduledState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public Guid? TimeoutTokenId { get; set; }

        public TimeSpan Delay { get; set; }
    }

    public sealed class ScheduledMachine : ViciOneServiceBusStateMachine<ScheduledState>
    {
        public ScheduledMachine(ScheduleObservation scheduleObservation)
        {
            ArgumentNullException.ThrowIfNull(scheduleObservation);
            InstanceState(instance => instance.CurrentState);
            Schedule(
                () => Timeout,
                instance => instance.TimeoutTokenId,
                configuration => configuration.Delay = TimeSpan.FromDays(1));
            Initially(
                When(Start)
                    .Then(context => context.Saga.Delay = context.Message.Delay)
                    .Schedule(
                        Timeout,
                        context => new TimeoutNotice(context.Saga.CorrelationId),
                        context => scheduleObservation.CreateDeadline(context.Saga.Delay))
                    .TransitionTo(Waiting));
            During(
                Waiting,
                When(Timeout.Received)
                    .Publish(context => new ScheduleCompleted(context.Saga.CorrelationId, "expired"))
                    .Finalize());
            SetCompletedWhenFinalized();
        }

        public State Waiting { get; } = null!;

        public Event<ScheduleStart> Start { get; } = null!;

        public Schedule<ScheduledState, TimeoutNotice> Timeout { get; } = null!;
    }

    public sealed class ScheduleObservation
    {
        public TaskCompletionSource<DateTime> Expected { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<DateTime> Actual { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public DateTime CreateDeadline(TimeSpan delay)
        {
            DateTime deadline = DateTime.UtcNow + delay;
            Expected.TrySetResult(deadline);
            return deadline;
        }
    }

    public sealed class RecordingSchedulerFilter<T>(ScheduleObservation observation) : IFilter<ConsumeContext<T>>
        where T : class
    {
        public async Task SendAsync(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            if (context.TryGetPayload(out MessageSchedulerContext? schedulerContext))
            {
                MessageSchedulerContext proxy = DispatchProxy.Create<MessageSchedulerContext, RecordingSchedulerContextProxy>();
                var recordingProxy = (RecordingSchedulerContextProxy)(object)proxy;
                recordingProxy.Inner = schedulerContext;
                recordingProxy.Observation = observation;
                context.AddOrUpdatePayload<MessageSchedulerContext>(() => proxy, _ => proxy);
            }

            await next.SendAsync(context).ConfigureAwait(false);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("recordScheduledDeadline");
    }

    private class RecordingSchedulerContextProxy : DispatchProxy
    {
        public required MessageSchedulerContext Inner { get; set; }

        public required ScheduleObservation Observation { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);

            if (targetMethod.Name == nameof(MessageSchedulerContext.ScheduleSendAsync)
                && args.FirstOrDefault(argument => argument is DateTimeOffset) is DateTimeOffset dueAt)
                Observation.Actual.TrySetResult(dueAt.UtcDateTime);

            try
            {
                return targetMethod.Invoke(Inner, args);
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }
}
