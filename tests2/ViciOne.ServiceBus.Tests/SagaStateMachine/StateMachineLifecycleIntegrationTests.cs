using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineLifecycleIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-LIFECYCLE", "correlated-topology-and-property-convention-matrix")]
    public async Task CorrelationConventionMatrix_CreatesRunsAndFinalizesEveryInstanceExactly()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        MessageCorrelation.UseCorrelationId<MappedStart>(message => message.ServiceId);
        MessageCorrelation.UseCorrelationId<MappedStop>(message => message.ServiceId);
        using var harness = CreateHarness("correlation-matrix", timeout);
        var machine = new ConventionMachine();
        ISagaStateMachineTestHarness<ConventionMachine, ConventionState> sagaHarness =
            harness.StateMachineSaga<ConventionState, ConventionMachine>(machine);

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlatedId = NewId.NextGuid();
            Guid mappedId = NewId.NextGuid();
            Guid conventionalId = NewId.NextGuid();

            await harness.InputQueueSendEndpoint.Send(new CorrelatedStart(correlatedId), cancellationToken);
            await harness.InputQueueSendEndpoint.Send(new MappedStart(mappedId), cancellationToken);
            await harness.InputQueueSendEndpoint.Send(new ConventionalStart(conventionalId), cancellationToken);

            Assert.Equal(correlatedId, await sagaHarness.Exists(correlatedId, machine.Running, timeout));
            Assert.Equal(mappedId, await sagaHarness.Exists(mappedId, machine.Running, timeout));
            Assert.Equal(conventionalId, await sagaHarness.Exists(conventionalId, machine.Running, timeout));

            await harness.InputQueueSendEndpoint.Send(new CorrelatedStop(correlatedId), cancellationToken);
            await harness.InputQueueSendEndpoint.Send(new MappedStop(mappedId), cancellationToken);
            await harness.InputQueueSendEndpoint.Send(new ConventionalStop(conventionalId), cancellationToken);

            Assert.Equal(correlatedId, await sagaHarness.Exists(correlatedId, machine.Final, timeout));
            Assert.Equal(mappedId, await sagaHarness.Exists(mappedId, machine.Final, timeout));
            Assert.Equal(conventionalId, await sagaHarness.Exists(conventionalId, machine.Final, timeout));

            AssertInstance(sagaHarness.Sagas.Contains(correlatedId), CorrelationPath.Correlated, machine.Final.Name);
            AssertInstance(sagaHarness.Sagas.Contains(mappedId), CorrelationPath.MessageTopology, machine.Final.Name);
            AssertInstance(sagaHarness.Sagas.Contains(conventionalId), CorrelationPath.PropertyConvention, machine.Final.Name);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Consumed.Select<CorrelatedStart>(SnapshotOnlyToken()));
        Assert.Single(harness.Consumed.Select<CorrelatedStop>(SnapshotOnlyToken()));
        Assert.Single(harness.Consumed.Select<MappedStart>(SnapshotOnlyToken()));
        Assert.Single(harness.Consumed.Select<MappedStop>(SnapshotOnlyToken()));
        Assert.Single(harness.Consumed.Select<ConventionalStart>(SnapshotOnlyToken()));
        Assert.Single(harness.Consumed.Select<ConventionalStop>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "business-key-select-id-insert-and-factory")]
    public async Task BusinessKeyCorrelation_UsesTheFactoryAndFindsTheSameInstanceThroughFinal()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("business-key", timeout);
        var machine = new BusinessKeyMachine();
        ISagaStateMachineTestHarness<BusinessKeyMachine, BusinessKeyState> sagaHarness =
            harness.StateMachineSaga<BusinessKeyState, BusinessKeyMachine>(machine);

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid transactionId = NewId.NextGuid();

            await harness.Bus.Publish(new BeginTransaction(transactionId), cancellationToken);
            IList<Guid> active = await sagaHarness.Exists(
                instance => instance.TransactionId == transactionId,
                machine.Active,
                timeout);
            BusinessKeyState instance = sagaHarness.Sagas.Contains(Assert.Single(active));

            Assert.NotNull(instance);
            Assert.Equal(transactionId, instance.CorrelationId);
            Assert.Equal(transactionId, instance.TransactionId);
            Assert.Equal("factory", instance.CreatedBy);
            Assert.Equal(1, instance.BeginCount);

            await harness.Bus.Publish(new CommitTransaction(transactionId), cancellationToken);
            IList<Guid> final = await sagaHarness.Exists(
                candidate => candidate.TransactionId == transactionId,
                machine.Final,
                timeout);

            Assert.Equal(active, final);
            Assert.Equal(machine.Final.Name, instance.CurrentState);
            Assert.Equal(1, instance.CommitCount);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-LIFECYCLE", "running-final-and-initial-response-removal")]
    public async Task CompletedInstance_IsRemovedAfterBothRunningAndInitialFinalizationPaths()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("completed-removal", timeout);
        var machine = new RemovingMachine();
        var repository = new InMemorySagaRepository<RemovingState>();
        ISagaStateMachineTestHarness<RemovingMachine, RemovingState> sagaHarness =
            harness.StateMachineSaga<RemovingState, RemovingMachine>(machine, repository);

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid runningId = NewId.NextGuid();
            await harness.InputQueueSendEndpoint.Send(new RemovalStart(runningId), cancellationToken);
            Assert.Equal(runningId, await sagaHarness.Exists(runningId, machine.Running, timeout));
            Assert.Equal(1, sagaHarness.Sagas.Contains(runningId).StartCount);

            Task<IReceivedMessage<RemovalStop>> stop = sagaHarness.Consumed
                .SelectAsync<RemovalStop>(cancellationToken)
                .First();
            await harness.InputQueueSendEndpoint.Send(new RemovalStop(runningId), cancellationToken);
            Assert.Null((await stop.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Null(await repository.Load(runningId));

            Guid immediateId = NewId.NextGuid();
            Task<IReceivedMessage<ImmediateRemovalRequest>> immediate = sagaHarness.Consumed
                .SelectAsync<ImmediateRemovalRequest>(cancellationToken)
                .First();
            IRequestClient<ImmediateRemovalRequest> client = harness.CreateRequestClient<ImmediateRemovalRequest>();
            Response<ImmediateRemovalResponse> response = await client.GetResponse<ImmediateRemovalResponse>(
                new ImmediateRemovalRequest(immediateId),
                cancellationToken);

            Assert.Equal(immediateId, response.Message.CorrelationId);
            Assert.Equal("removed-from-initial", response.Message.Result);
            Assert.Null((await immediate.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Null(await repository.Load(immediateId));
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void AssertInstance(ConventionState instance, CorrelationPath path, string expectedState)
    {
        Assert.NotNull(instance);
        Assert.Equal(path, instance.Path);
        Assert.Equal(expectedState, instance.CurrentState);
        Assert.Equal(1, instance.StartCount);
        Assert.Equal(1, instance.StopCount);
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

    public enum CorrelationPath
    {
        Correlated,
        MessageTopology,
        PropertyConvention,
    }

    public sealed record CorrelatedStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record CorrelatedStop(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record MappedStart(Guid ServiceId);

    public sealed record MappedStop(Guid ServiceId);

    public sealed record ConventionalStart(Guid CorrelationId);

    public sealed record ConventionalStop(Guid CorrelationId);

    public sealed class ConventionState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public CorrelationPath Path { get; set; }

        public int StartCount { get; set; }

        public int StopCount { get; set; }
    }

    public sealed class ConventionMachine : ViciOneServiceBusStateMachine<ConventionState>
    {
        public ConventionMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Initially(
                When(CorrelatedStarted).Then(context => RecordStart(context.Saga, CorrelationPath.Correlated)).TransitionTo(Running),
                When(MappedStarted).Then(context => RecordStart(context.Saga, CorrelationPath.MessageTopology)).TransitionTo(Running),
                When(ConventionalStarted).Then(context => RecordStart(context.Saga, CorrelationPath.PropertyConvention)).TransitionTo(Running));
            During(
                Running,
                When(CorrelatedStopped).Then(context => context.Saga.StopCount++).Finalize(),
                When(MappedStopped).Then(context => context.Saga.StopCount++).Finalize(),
                When(ConventionalStopped).Then(context => context.Saga.StopCount++).Finalize());
        }

        private static void RecordStart(ConventionState state, CorrelationPath path)
        {
            state.Path = path;
            state.StartCount++;
        }

        public State Running { get; } = null!;

        public Event<CorrelatedStart> CorrelatedStarted { get; } = null!;

        public Event<CorrelatedStop> CorrelatedStopped { get; } = null!;

        public Event<MappedStart> MappedStarted { get; } = null!;

        public Event<MappedStop> MappedStopped { get; } = null!;

        public Event<ConventionalStart> ConventionalStarted { get; } = null!;

        public Event<ConventionalStop> ConventionalStopped { get; } = null!;
    }

    public sealed record BeginTransaction(Guid TransactionId);

    public sealed record CommitTransaction(Guid TransactionId);

    public sealed class BusinessKeyState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public Guid TransactionId { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public int BeginCount { get; set; }

        public int CommitCount { get; set; }
    }

    public sealed class BusinessKeyMachine : ViciOneServiceBusStateMachine<BusinessKeyState>
    {
        public BusinessKeyMachine()
        {
            InstanceState(instance => instance.CurrentState);
            Event(() => Begin, configuration =>
            {
                configuration.CorrelateById(instance => instance.TransactionId, context => context.Message.TransactionId);
                configuration.SelectId(context => context.Message.TransactionId);
                configuration.InsertOnInitial = true;
                configuration.SetSagaFactory(context => new BusinessKeyState
                {
                    CorrelationId = context.Message.TransactionId,
                    TransactionId = context.Message.TransactionId,
                    CreatedBy = "factory",
                });
            });
            Event(() => Commit, configuration => configuration
                .CorrelateById(instance => instance.TransactionId, context => context.Message.TransactionId));
            Initially(When(Begin).Then(context => context.Saga.BeginCount++).TransitionTo(Active));
            During(Active, When(Commit).Then(context => context.Saga.CommitCount++).Finalize());
        }

        public State Active { get; } = null!;

        public Event<BeginTransaction> Begin { get; } = null!;

        public Event<CommitTransaction> Commit { get; } = null!;
    }

    public sealed record RemovalStart(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record RemovalStop(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ImmediateRemovalRequest(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ImmediateRemovalResponse(Guid CorrelationId, string Result) : CorrelatedBy<Guid>;

    public sealed class RemovingState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public int StartCount { get; set; }
    }

    public sealed class RemovingMachine : ViciOneServiceBusStateMachine<RemovingState>
    {
        public RemovingMachine()
        {
            InstanceState(instance => instance.CurrentState);
            SetCompletedWhenFinalized();
            Initially(
                When(Start).Then(context => context.Saga.StartCount++).TransitionTo(Running),
                When(Immediate)
                    .Respond(context => new ImmediateRemovalResponse(context.Saga.CorrelationId, "removed-from-initial"))
                    .Finalize());
            During(Running, When(Stop).Finalize());
        }

        public State Running { get; } = null!;

        public Event<RemovalStart> Start { get; } = null!;

        public Event<RemovalStop> Stop { get; } = null!;

        public Event<ImmediateRemovalRequest> Immediate { get; } = null!;
    }
}
