using Quartz;
using System.Collections.Concurrent;
using ViciOne.ServiceBus.QuartzIntegration.Tests.Testing;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.QuartzIntegration.Tests.QuartzIntegration;

[Collection(QuartzIntegrationCollection.Name)]
public sealed class QuartzSagaSchedulingIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-RESCHEDULE", "replace-cancel-and-finalize")]
    public async Task Rescheduling_ReplacesTheOldTriggerAndFinalizationCancelsTheReplacement()
    {
        TimeSpan timeout = OperationTimeout();
        Guid correlationId = NewId.NextGuid();
        string queueName = $"quartz-saga-reschedule-{NewId.NextGuid():N}";
        var inputAddress = new Uri($"loopback://localhost/{queueName}");
        var repository = new InMemorySagaRepository<RescheduleState>();
        var stateMachine = new RescheduleStateMachine();
        var stopped = new TaskCompletionSource<RescheduleStopped>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.Start(
            timeout,
            configure: configurator =>
            {
                configurator.ReceiveEndpoint(queueName, endpoint =>
                {
                    endpoint.UseInMemoryOutbox();
                    endpoint.StateMachineSaga(stateMachine, repository);
                });
                configurator.ReceiveEndpoint($"{queueName}-events", endpoint => endpoint.Handler<RescheduleStopped>(context =>
                {
                    stopped.TrySetResult(context.Message);
                    return Task.CompletedTask;
                }));
            });
        var scheduled = new ConsumeCompletionObserver<ScheduleMessage>(_ => true, 2);
        var canceled = new ConsumeCompletionObserver<CancelScheduledMessage>(_ => true, 2);
        var starts = new ConsumeCompletionObserver<StartReschedule>(message => message.CorrelationId == correlationId);
        var refreshes = new ConsumeCompletionObserver<RefreshSchedule>(message => message.CorrelationId == correlationId);
        using ConnectHandle scheduledObserver = fixture.Bus.ConnectConsumeObserver(scheduled);
        using ConnectHandle canceledObserver = fixture.Bus.ConnectConsumeObserver(canceled);
        using ConnectHandle startObserver = fixture.Bus.ConnectConsumeObserver(starts);
        using ConnectHandle refreshObserver = fixture.Bus.ConnectConsumeObserver(refreshes);
        ISendEndpoint input = await fixture.Bus.GetSendEndpoint(inputAddress)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        await input.Send(new StartReschedule(correlationId), TestContext.Current.CancellationToken);
        await starts.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        Guid firstToken = Assert.IsType<Guid>(repository[correlationId].Instance.ScheduleTokenId);
        TriggerKey firstTrigger = new(firstToken.ToString("N"));
        Assert.True(await fixture.Scheduler.CheckExists(firstTrigger, TestContext.Current.CancellationToken));

        await input.Send(new RefreshSchedule(correlationId), TestContext.Current.CancellationToken);
        await refreshes.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await scheduled.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        Guid replacementToken = Assert.IsType<Guid>(repository[correlationId].Instance.ScheduleTokenId);
        TriggerKey replacementTrigger = new(replacementToken.ToString("N"));

        Assert.NotEqual(firstToken, replacementToken);
        Assert.False(await fixture.Scheduler.CheckExists(firstTrigger, TestContext.Current.CancellationToken));
        Assert.True(await fixture.Scheduler.CheckExists(replacementTrigger, TestContext.Current.CancellationToken));

        await input.Send(new StopReschedule(correlationId), TestContext.Current.CancellationToken);
        RescheduleStopped final = await stopped.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await canceled.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(correlationId, final.CorrelationId);
        Assert.False(await fixture.Scheduler.CheckExists(replacementTrigger, TestContext.Current.CancellationToken));
        Assert.Equal(0, repository.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-SCHEDULE", "scheduled-message-correlates-and-finalizes")]
    public async Task ScheduledMessage_CorrelatesToItsSagaAndFinalizesIt()
    {
        TimeSpan timeout = OperationTimeout();
        Guid correlationId = NewId.NextGuid();
        string queueName = $"quartz-saga-fire-{NewId.NextGuid():N}";
        var inputAddress = new Uri($"loopback://localhost/{queueName}");
        var repository = new InMemorySagaRepository<RescheduleState>();
        var stateMachine = new RescheduleStateMachine();
        var fired = new TaskCompletionSource<ScheduleFired>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.Start(
            timeout,
            configure: configurator =>
            {
                configurator.ReceiveEndpoint(queueName, endpoint =>
                {
                    endpoint.UseInMemoryOutbox();
                    endpoint.StateMachineSaga(stateMachine, repository);
                });
                configurator.ReceiveEndpoint($"{queueName}-events", endpoint => endpoint.Handler<ScheduleFired>(context =>
                {
                    fired.TrySetResult(context.Message);
                    return Task.CompletedTask;
                }));
            });
        var scheduled = new ConsumeCompletionObserver<ScheduleMessage>(_ => true);
        var starts = new ConsumeCompletionObserver<StartReschedule>(message => message.CorrelationId == correlationId);
        using ConnectHandle scheduledObserver = fixture.Bus.ConnectConsumeObserver(scheduled);
        using ConnectHandle startObserver = fixture.Bus.ConnectConsumeObserver(starts);
        ISendEndpoint input = await fixture.Bus.GetSendEndpoint(inputAddress)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        await input.Send(new StartReschedule(correlationId), TestContext.Current.CancellationToken);
        await starts.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await scheduled.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        Guid token = Assert.IsType<Guid>(repository[correlationId].Instance.ScheduleTokenId);
        ITrigger trigger = Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            new TriggerKey(token.ToString("N")),
            TestContext.Current.CancellationToken));

        await fixture.Scheduler.TriggerJob(trigger.JobKey, trigger.JobDataMap, TestContext.Current.CancellationToken);
        ScheduleFired delivered = await fired.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(correlationId, delivered.CorrelationId);
        Assert.Equal(token, delivered.ScheduleTokenId);
        Assert.Equal(0, repository.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-MULTIPLE-SCHEDULES", "independent-token-and-delivery")]
    public async Task Saga_CanOwnAndReceiveTwoIndependentSchedules()
    {
        TimeSpan timeout = OperationTimeout();
        Guid correlationId = NewId.NextGuid();
        string queueName = $"quartz-saga-multiple-{NewId.NextGuid():N}";
        var inputAddress = new Uri($"loopback://localhost/{queueName}");
        var repository = new InMemorySagaRepository<MultipleScheduleState>();
        var stateMachine = new MultipleScheduleStateMachine();
        var firstFired = new TaskCompletionSource<FirstScheduleFired>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondFired = new TaskCompletionSource<SecondScheduleFired>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.Start(
            timeout,
            configure: configurator =>
            {
                configurator.ReceiveEndpoint(queueName, endpoint =>
                {
                    endpoint.UseInMemoryOutbox();
                    endpoint.StateMachineSaga(stateMachine, repository);
                });
                configurator.ReceiveEndpoint($"{queueName}-events", endpoint =>
                {
                    endpoint.Handler<FirstScheduleFired>(context =>
                    {
                        firstFired.TrySetResult(context.Message);
                        return Task.CompletedTask;
                    });
                    endpoint.Handler<SecondScheduleFired>(context =>
                    {
                        secondFired.TrySetResult(context.Message);
                        return Task.CompletedTask;
                    });
                });
            });
        var scheduled = new ConsumeCompletionObserver<ScheduleMessage>(_ => true, 2);
        var starts = new ConsumeCompletionObserver<StartMultipleSchedules>(message => message.CorrelationId == correlationId);
        using ConnectHandle scheduledObserver = fixture.Bus.ConnectConsumeObserver(scheduled);
        using ConnectHandle startObserver = fixture.Bus.ConnectConsumeObserver(starts);
        ISendEndpoint input = await fixture.Bus.GetSendEndpoint(inputAddress)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);

        await input.Send(new StartMultipleSchedules(correlationId), TestContext.Current.CancellationToken);
        await starts.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await scheduled.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        MultipleScheduleState saga = repository[correlationId].Instance;
        Guid firstToken = Assert.IsType<Guid>(saga.FirstTokenId);
        Guid secondToken = Assert.IsType<Guid>(saga.SecondTokenId);
        ITrigger firstTrigger = await GetTrigger(fixture, firstToken);
        ITrigger secondTrigger = await GetTrigger(fixture, secondToken);

        Assert.NotEqual(firstToken, secondToken);
        await fixture.Scheduler.TriggerJob(firstTrigger.JobKey, firstTrigger.JobDataMap, TestContext.Current.CancellationToken);
        FirstScheduleFired first = await firstFired.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(correlationId, first.CorrelationId);
        Assert.Equal(1, repository.Count);
        Assert.True(await fixture.Scheduler.CheckExists(secondTrigger.Key, TestContext.Current.CancellationToken));

        await fixture.Scheduler.TriggerJob(secondTrigger.JobKey, secondTrigger.JobDataMap, TestContext.Current.CancellationToken);
        SecondScheduleFired second = await secondFired.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(correlationId, second.CorrelationId);
        Assert.Equal(0, repository.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-QUARTZ-SAGA-LOAD", "concurrent-schedules-finalize-and-remove-all-sagas")]
    public async Task ConcurrentScheduledSagas_FinalizeAndLeaveNoStoredInstances()
    {
        const int sagaCount = 20;
        TimeSpan timeout = OperationTimeout();
        string queueName = $"quartz-saga-load-{NewId.NextGuid():N}";
        var inputAddress = new Uri($"loopback://localhost/{queueName}");
        var repository = new InMemorySagaRepository<LoadState>();
        var stateMachine = new LoadStateMachine();
        var completedIds = new ConcurrentDictionary<Guid, byte>();
        var allCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using QuartzTestBus fixture = await QuartzTestBus.Start(
            timeout,
            configure: configurator =>
            {
                configurator.ReceiveEndpoint(queueName, endpoint =>
                {
                    endpoint.UseInMemoryOutbox();
                    endpoint.StateMachineSaga(stateMachine, repository);
                });
                configurator.ReceiveEndpoint($"{queueName}-events", endpoint => endpoint.Handler<LoadStopped>(context =>
                {
                    completedIds.TryAdd(context.Message.CorrelationId, 0);
                    if (completedIds.Count == sagaCount)
                        allCompleted.TrySetResult();

                    return Task.CompletedTask;
                }));
            });
        var scheduled = new ConsumeCompletionObserver<ScheduleMessage>(_ => true, sagaCount);
        using ConnectHandle scheduledObserver = fixture.Bus.ConnectConsumeObserver(scheduled);
        ISendEndpoint input = await fixture.Bus.GetSendEndpoint(inputAddress)
            .WaitAsync(timeout, TestContext.Current.CancellationToken);
        Guid[] correlationIds = Enumerable.Range(0, sagaCount).Select(_ => NewId.NextGuid()).ToArray();

        await Task.WhenAll(correlationIds.Select(correlationId =>
            input.Send(new StartLoad(correlationId), TestContext.Current.CancellationToken)));
        await scheduled.Completed.WaitAsync(timeout, TestContext.Current.CancellationToken);
        await allCompleted.Task.WaitAsync(timeout, TestContext.Current.CancellationToken);

        Assert.Equal(sagaCount, scheduled.ObservedCount);
        Assert.Equal(correlationIds.Order(), completedIds.Keys.Order());
        Assert.Equal(0, repository.Count);
    }

    private static async Task<ITrigger> GetTrigger(QuartzTestBus fixture, Guid tokenId) =>
        Assert.IsAssignableFrom<ITrigger>(await fixture.Scheduler.GetTrigger(
            new TriggerKey(tokenId.ToString("N")),
            TestContext.Current.CancellationToken));

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed class RescheduleState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public State CurrentState { get; set; } = null!;
        public Guid? ScheduleTokenId { get; set; }
    }

    public sealed class RescheduleStateMachine : ViciOneServiceBusStateMachine<RescheduleState>
    {
        public RescheduleStateMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Started, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Event(() => Refreshed, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Event(() => Stopped, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Schedule(() => Due, instance => instance.ScheduleTokenId, configuration =>
            {
                configuration.Delay = TimeSpan.FromDays(3650);
                configuration.Received = received => received.CorrelateById(context => context.Message.CorrelationId);
            });

            Initially(
                When(Started)
                    .Schedule(Due, context => new ScheduleDue(context.Saga.CorrelationId))
                    .TransitionTo(Active));
            During(Active,
                When(Refreshed)
                    .Schedule(Due, context => new ScheduleDue(context.Saga.CorrelationId)),
                When(Due.Received)
                    .Publish(context => new ScheduleFired(context.Saga.CorrelationId, context.Saga.ScheduleTokenId))
                    .Finalize(),
                When(Stopped)
                    .Unschedule(Due)
                    .Publish(context => new RescheduleStopped(context.Saga.CorrelationId))
                    .Finalize());
            SetCompletedWhenFinalized();
        }

        public State Active { get; private set; } = null!;
        public Event<StartReschedule> Started { get; private set; } = null!;
        public Event<RefreshSchedule> Refreshed { get; private set; } = null!;
        public Event<StopReschedule> Stopped { get; private set; } = null!;
        public Schedule<RescheduleState, ScheduleDue> Due { get; private set; } = null!;
    }

    public sealed class MultipleScheduleState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public State CurrentState { get; set; } = null!;
        public Guid? FirstTokenId { get; set; }
        public Guid? SecondTokenId { get; set; }
    }

    public sealed class MultipleScheduleStateMachine : ViciOneServiceBusStateMachine<MultipleScheduleState>
    {
        public MultipleScheduleStateMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Started, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Schedule(() => First, instance => instance.FirstTokenId, configuration =>
            {
                configuration.Delay = TimeSpan.FromDays(3650);
                configuration.Received = received => received.CorrelateById(context => context.Message.CorrelationId);
            });
            Schedule(() => Second, instance => instance.SecondTokenId, configuration =>
            {
                configuration.Delay = TimeSpan.FromDays(3651);
                configuration.Received = received => received.CorrelateById(context => context.Message.CorrelationId);
            });

            Initially(
                When(Started)
                    .Schedule(First, context => new FirstDue(context.Saga.CorrelationId))
                    .Schedule(Second, context => new SecondDue(context.Saga.CorrelationId))
                    .TransitionTo(Waiting));
            During(Waiting,
                When(First.Received)
                    .Publish(context => new FirstScheduleFired(context.Saga.CorrelationId))
                    .TransitionTo(FirstCompleted));
            During(FirstCompleted,
                When(Second.Received)
                    .Publish(context => new SecondScheduleFired(context.Saga.CorrelationId))
                    .Finalize());
            SetCompletedWhenFinalized();
        }

        public State Waiting { get; private set; } = null!;
        public State FirstCompleted { get; private set; } = null!;
        public Event<StartMultipleSchedules> Started { get; private set; } = null!;
        public Schedule<MultipleScheduleState, FirstDue> First { get; private set; } = null!;
        public Schedule<MultipleScheduleState, SecondDue> Second { get; private set; } = null!;
    }

    public sealed class LoadState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public State CurrentState { get; set; } = null!;
        public Guid? StopTokenId { get; set; }
    }

    public sealed class LoadStateMachine : ViciOneServiceBusStateMachine<LoadState>
    {
        public LoadStateMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Started, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Schedule(() => Stop, instance => instance.StopTokenId, configuration =>
            {
                configuration.Delay = TimeSpan.Zero;
                configuration.Received = received => received.CorrelateById(context => context.Message.CorrelationId);
            });

            Initially(
                When(Started)
                    .Schedule(Stop, context => new LoadDue(context.Saga.CorrelationId))
                    .TransitionTo(Running));
            During(Running,
                When(Stop.Received)
                    .Publish(context => new LoadStopped(context.Saga.CorrelationId))
                    .Finalize());
            SetCompletedWhenFinalized();
        }

        public State Running { get; private set; } = null!;
        public Event<StartLoad> Started { get; private set; } = null!;
        public Schedule<LoadState, LoadDue> Stop { get; private set; } = null!;
    }

    public sealed record StartReschedule(Guid CorrelationId);
    public sealed record RefreshSchedule(Guid CorrelationId);
    public sealed record StopReschedule(Guid CorrelationId);
    public sealed record ScheduleDue(Guid CorrelationId);
    public sealed record ScheduleFired(Guid CorrelationId, Guid? ScheduleTokenId);
    public sealed record RescheduleStopped(Guid CorrelationId);
    public sealed record StartMultipleSchedules(Guid CorrelationId);
    public sealed record FirstDue(Guid CorrelationId);
    public sealed record SecondDue(Guid CorrelationId);
    public sealed record FirstScheduleFired(Guid CorrelationId);
    public sealed record SecondScheduleFired(Guid CorrelationId);
    public sealed record StartLoad(Guid CorrelationId);
    public sealed record LoadDue(Guid CorrelationId);
    public sealed record LoadStopped(Guid CorrelationId);
}
