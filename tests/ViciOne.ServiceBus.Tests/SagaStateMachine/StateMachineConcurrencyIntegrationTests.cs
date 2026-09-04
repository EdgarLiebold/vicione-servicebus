using System.Collections.Concurrent;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineConcurrencyIntegrationTests
{
    private const int PartitionCount = 100;

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONCURRENCY", "awaited-enter-finalization-removes-instance")]
    public async Task AwaitedEnterActivity_FinalizesAndRemovesOnlyAfterTheExternalDecisionCompletes()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var decision = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("awaited-finalize", timeout);
        var machine = new AwaitedFinalizeMachine(decision.Task, entered);
        var repository = new InMemorySagaRepository<AwaitedFinalizeState>();
        ISagaStateMachineTestHarness<AwaitedFinalizeMachine, AwaitedFinalizeState> sagaHarness =
            harness.StateMachineSaga<AwaitedFinalizeState, AwaitedFinalizeMachine>(machine, repository);

        Guid correlationId = NewId.NextGuid();
        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Task<IReceivedMessage<AwaitedFinalizeStart>> consumed = sagaHarness.Consumed
                .SelectAsync<AwaitedFinalizeStart>(cancellationToken)
                .First();
            await harness.InputQueueSendEndpoint.Send(new AwaitedFinalizeStart(correlationId), cancellationToken);
            await entered.Task.WaitAsync(timeout, cancellationToken);

            AwaitedFinalizeState inFlight = sagaHarness.Sagas.Contains(correlationId);
            Assert.NotNull(inFlight);
            Assert.Equal(machine.PendingDecision.Name, inFlight.CurrentState);
            Assert.True(inFlight.ReceivedFirst);
            Assert.False(inFlight.Decision);
            Assert.False(consumed.IsCompleted);

            decision.TrySetResult(true);
            Assert.Null((await consumed.WaitAsync(timeout, cancellationToken)).Exception);
        }
        finally
        {
            decision.TrySetResult(true);
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Null(await repository.Load(correlationId));
        Assert.Single(sagaHarness.Consumed.Select<AwaitedFinalizeStart>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONCURRENCY", "same-instance-wait-does-not-block-unrelated-instance")]
    public async Task HeldInstanceLock_DoesNotBlockAnotherInstanceAndReleasesTheQueuedSameInstanceMessage()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var releaseCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completionEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelDispatched = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("repository-progress", timeout);
        var machine = new RepositoryProgressMachine(releaseCompletion.Task, completionEntered);
        var repository = new InMemorySagaRepository<RepositoryProgressState>();
        var signalingRepository = new SignalingSagaRepository<RepositoryProgressState, RepositoryCancel>(repository, cancelDispatched);
        ISagaStateMachineTestHarness<RepositoryProgressMachine, RepositoryProgressState> sagaHarness =
            harness.StateMachineSaga<RepositoryProgressState, RepositoryProgressMachine>(machine, signalingRepository);

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid heldId = NewId.NextGuid();
            Guid independentId = NewId.NextGuid();
            await harness.InputQueueSendEndpoint.Send(new RepositoryCreate(heldId), cancellationToken);
            Assert.Equal(heldId, await sagaHarness.Exists(heldId, machine.Active, timeout));

            Task<IReceivedMessage<RepositoryComplete>> completed = sagaHarness.Consumed
                .SelectAsync<RepositoryComplete>(cancellationToken)
                .First();
            Task<IReceivedMessage<RepositoryCancel>> canceled = sagaHarness.Consumed
                .SelectAsync<RepositoryCancel>(cancellationToken)
                .First();
            await harness.InputQueueSendEndpoint.Send(new RepositoryComplete(heldId), cancellationToken);
            await completionEntered.Task.WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.Send(new RepositoryCancel(heldId), cancellationToken);
            Task cancelDispatch = await cancelDispatched.Task.WaitAsync(timeout, cancellationToken);
            Assert.False(cancelDispatch.IsCompleted);
            await harness.InputQueueSendEndpoint.Send(new RepositoryCreate(independentId), cancellationToken);

            Assert.Equal(independentId, await sagaHarness.Exists(independentId, machine.Active, timeout));
            Assert.False(completed.IsCompleted);

            releaseCompletion.TrySetResult();
            Assert.Null((await completed.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Null((await canceled.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Null(await repository.Load(heldId));
            Assert.Equal(independentId, await sagaHarness.Exists(independentId, machine.Active, timeout));
        }
        finally
        {
            releaseCompletion.TrySetResult();
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(2, sagaHarness.Consumed.Select<RepositoryCreate>(SnapshotOnlyToken()).Count());
        Assert.Single(sagaHarness.Consumed.Select<RepositoryComplete>(SnapshotOnlyToken()));
        Assert.Single(sagaHarness.Consumed.Select<RepositoryCancel>(SnapshotOnlyToken()));
    }

    sealed class SignalingSagaRepository<TSaga, TMessage> :
        ISagaRepository<TSaga>,
        IQuerySagaRepository<TSaga>,
        ILoadSagaRepository<TSaga>
        where TSaga : class, ISaga
        where TMessage : class
    {
        readonly TaskCompletionSource<Task> _dispatched;
        readonly InMemorySagaRepository<TSaga> _repository;

        public SignalingSagaRepository(InMemorySagaRepository<TSaga> repository, TaskCompletionSource<Task> dispatched)
        {
            _repository = repository;
            _dispatched = dispatched;
        }

        public Task<TSaga> Load(Guid correlationId) => _repository.Load(correlationId);

        public Task<IEnumerable<Guid>> Find(ISagaQuery<TSaga> query) => _repository.Find(query);

        public void Probe(ProbeContext context) => ((IProbeSite)_repository).Probe(context);

        public Task Send<T>(ConsumeContext<T> context, ISagaPolicy<TSaga, T> policy, IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class
        {
            Task dispatch = ((ISagaRepository<TSaga>)_repository).Send(context, policy, next);
            if (context.Message is TMessage)
                _dispatched.TrySetResult(dispatch);

            return dispatch;
        }

        public Task SendQuery<T>(ConsumeContext<T> context, ISagaQuery<TSaga> query, ISagaPolicy<TSaga, T> policy,
            IPipe<SagaConsumeContext<TSaga, T>> next)
            where T : class
        {
            return ((ISagaRepository<TSaga>)_repository).SendQuery(context, query, policy, next);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-PARTITION", "one-hundred-keys-with-nested-message-pipe")]
    public async Task FourWayPartitioner_CreatesAllOneHundredInstancesAndRunsTheNestedMessagePipeExactlyOnce()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("partitioning", timeout);
        var machine = new PartitionMachine();
        var repository = new InMemorySagaRepository<PartitionState>();
        var recorder = new PartitionPipeRecorder();
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.StateMachineSaga(
            machine,
            repository,
            saga =>
            {
                saga.Message<PartitionStart>(message =>
                    message.UsePartitioner(4, context => context.Message.CorrelationId));
                saga.SagaMessage<PartitionStart>(message => message.Message(pipe =>
                    pipe.UseExecute(context => recorder.Record(context.Message.CorrelationId))));
            });

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        Guid[] ids = Enumerable.Range(0, PartitionCount).Select(_ => NewId.NextGuid()).ToArray();
        try
        {
            Task[] publishes = ids
                .Select(id => harness.Bus.Publish(new PartitionStart(id), cancellationToken))
                .ToArray();
            await Task.WhenAll(publishes).WaitAsync(timeout, cancellationToken);

            Guid?[] located = await Task.WhenAll(ids.Select(id =>
                repository.ShouldContainSagaInState(id, machine, machine.Waiting, timeout)));
            Assert.Equal(ids.Order(), located.Select(id => Assert.NotNull(id)).Order());
            Assert.Equal(PartitionCount, recorder.Count);
            Assert.Equal(ids.Order(), recorder.Ids.Order());
            Assert.All(ids, id => Assert.Equal(1, recorder.Occurrences(id)));
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(PartitionCount, harness.Consumed.Select<PartitionStart>(SnapshotOnlyToken()).Count());
        Assert.Empty(harness.Published.Select<Fault<PartitionStart>>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CONCURRENCY", "four-voice-single-instance-composite")]
    public async Task ConcurrentVoices_UpdateOneInstanceWithoutLossAndReachHarmonyExactlyOnce()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("choir", timeout);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.UseMessageRetry(retry => retry.Immediate(5));
            endpoint.UseInMemoryOutbox();
        };
        var machine = new ChoirMachine();
        ISagaStateMachineTestHarness<ChoirMachine, ChoirState> sagaHarness =
            harness.StateMachineSaga<ChoirState, ChoirMachine>(machine);

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlationId = NewId.NextGuid();
            await harness.Bus.Publish(new RehearsalBegins(correlationId), cancellationToken);
            Assert.Equal(correlationId, await sagaHarness.Exists(correlationId, machine.Warmup, timeout));

            await Task.WhenAll(
                harness.Bus.Publish(new Bass(correlationId, "John"), cancellationToken),
                harness.Bus.Publish(new Baritone(correlationId, "Mark"), cancellationToken),
                harness.Bus.Publish(new Tenor(correlationId, "Anthony"), cancellationToken),
                harness.Bus.Publish(new Countertenor(correlationId, "Tom"), cancellationToken))
                .WaitAsync(timeout, cancellationToken);

            Assert.Equal(correlationId, await sagaHarness.Exists(correlationId, machine.Harmony, timeout));
            ChoirState instance = sagaHarness.Sagas.Contains(correlationId);
            Assert.NotNull(instance);
            Assert.Equal("John", instance.Bass);
            Assert.Equal("Mark", instance.Baritone);
            Assert.Equal("Anthony", instance.Tenor);
            Assert.Equal("Tom", instance.Countertenor);
            Assert.Equal(1, instance.HarmonyCount);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(sagaHarness.Consumed.Select<Bass>(SnapshotOnlyToken()));
        Assert.Single(sagaHarness.Consumed.Select<Baritone>(SnapshotOnlyToken()));
        Assert.Single(sagaHarness.Consumed.Select<Tenor>(SnapshotOnlyToken()));
        Assert.Single(sagaHarness.Consumed.Select<Countertenor>(SnapshotOnlyToken()));
        Assert.Empty(harness.Published.Select<Fault<Bass>>(SnapshotOnlyToken()));
        Assert.Empty(harness.Published.Select<Fault<Baritone>>(SnapshotOnlyToken()));
        Assert.Empty(harness.Published.Select<Fault<Tenor>>(SnapshotOnlyToken()));
        Assert.Empty(harness.Published.Select<Fault<Countertenor>>(SnapshotOnlyToken()));
    }

    private static InMemoryTestHarness CreateHarness(string prefix, TimeSpan timeout) =>
        new($"{prefix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record AwaitedFinalizeStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class AwaitedFinalizeState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public bool ReceivedFirst { get; set; }

        public bool Decision { get; set; }
    }

    public sealed class AwaitedFinalizeMachine : ViciOneServiceBusStateMachine<AwaitedFinalizeState>
    {
        public AwaitedFinalizeMachine(Task<bool> decision, TaskCompletionSource entered)
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(Start)
                    .Then(context => context.Saga.ReceivedFirst = true)
                    .TransitionTo(PendingDecision));
            WhenEnter(
                PendingDecision,
                behavior => behavior
                    .ThenAsync(async context =>
                    {
                        entered.TrySetResult();
                        context.Saga.Decision = await decision;
                    })
                    .If(context => context.Saga.ReceivedFirst && context.Saga.Decision, then => then.Finalize()));
            SetCompletedWhenFinalized();
        }

        public State PendingDecision { get; } = null!;

        public Event<AwaitedFinalizeStart> Start { get; } = null!;
    }

    public sealed record RepositoryCreate(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record RepositoryComplete(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record RepositoryCancel(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class RepositoryProgressState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class RepositoryProgressMachine : ViciOneServiceBusStateMachine<RepositoryProgressState>
    {
        public RepositoryProgressMachine(Task releaseCompletion, TaskCompletionSource completionEntered)
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Cancel, configuration => configuration.OnMissingInstance(missing => missing.Discard()));
            Initially(When(Create).TransitionTo(Active));
            During(
                Active,
                When(Complete)
                    .ThenAsync(async _ =>
                    {
                        completionEntered.TrySetResult();
                        await releaseCompletion;
                    })
                    .Finalize(),
                When(Cancel).Finalize());
            During(Final, Ignore(Cancel));
            SetCompletedWhenFinalized();
        }

        public State Active { get; } = null!;

        public Event<RepositoryCreate> Create { get; } = null!;

        public Event<RepositoryComplete> Complete { get; } = null!;

        public Event<RepositoryCancel> Cancel { get; } = null!;
    }

    public sealed record PartitionStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class PartitionState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;
    }

    public sealed class PartitionMachine : ViciOneServiceBusStateMachine<PartitionState>
    {
        public PartitionMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Start).TransitionTo(Waiting));
        }

        public State Waiting { get; } = null!;

        public Event<PartitionStart> Start { get; } = null!;
    }

    public sealed class PartitionPipeRecorder
    {
        private readonly ConcurrentDictionary<Guid, int> _occurrences = new();

        public int Count => _occurrences.Values.Sum();

        public Guid[] Ids => _occurrences.Keys.ToArray();

        public void Record(Guid correlationId) => _occurrences.AddOrUpdate(correlationId, 1, (_, count) => count + 1);

        public int Occurrences(Guid correlationId) => _occurrences.GetValueOrDefault(correlationId);
    }

    public sealed record RehearsalBegins(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record Bass(Guid CorrelationId, string Name) : CorrelatedBy<Guid>;

    public sealed record Baritone(Guid CorrelationId, string Name) : CorrelatedBy<Guid>;

    public sealed record Tenor(Guid CorrelationId, string Name) : CorrelatedBy<Guid>;

    public sealed record Countertenor(Guid CorrelationId, string Name) : CorrelatedBy<Guid>;

    public sealed class ChoirState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public int VoiceStatus { get; set; }

        public string Bass { get; set; } = string.Empty;

        public string Baritone { get; set; } = string.Empty;

        public string Tenor { get; set; } = string.Empty;

        public string Countertenor { get; set; } = string.Empty;

        public int HarmonyCount { get; set; }
    }

    public sealed class ChoirMachine : ViciOneServiceBusStateMachine<ChoirState>
    {
        public ChoirMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(When(Begin).TransitionTo(Warmup));
            During(
                Warmup,
                When(BassArrived).Then(context => context.Saga.Bass = context.Message.Name),
                When(BaritoneArrived).Then(context => context.Saga.Baritone = context.Message.Name),
                When(TenorArrived).Then(context => context.Saga.Tenor = context.Message.Name),
                When(CountertenorArrived).Then(context => context.Saga.Countertenor = context.Message.Name));
            CompositeEvent(
                () => AllVoices,
                instance => instance.VoiceStatus,
                CompositeEventOptions.RaiseOnce,
                BassArrived,
                BaritoneArrived,
                TenorArrived,
                CountertenorArrived);
            During(
                Warmup,
                When(AllVoices)
                    .Then(context => context.Saga.HarmonyCount++)
                    .TransitionTo(Harmony));
        }

        public State Warmup { get; } = null!;

        public State Harmony { get; } = null!;

        public Event<RehearsalBegins> Begin { get; } = null!;

        public Event<Bass> BassArrived { get; } = null!;

        public Event<Baritone> BaritoneArrived { get; } = null!;

        public Event<Tenor> TenorArrived { get; } = null!;

        public Event<Countertenor> CountertenorArrived { get; } = null!;

        public Event AllVoices { get; } = null!;
    }
}
