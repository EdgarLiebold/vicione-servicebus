using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineLifecycleIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-LIFECYCLE", "correlated-topology-and-property-convention-matrix")]
    public async Task CorrelationConventionMatrix_CreatesRunsAndFinalizesEveryInstanceExactlyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("correlation-matrix", timeout);
        var machine = new ConventionMachine();
        ISagaStateMachineTestHarness<ConventionMachine, ConventionState> sagaHarness =
            harness.AddSagaStateMachine<ConventionMachine, ConventionState>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlatedId = NewId.NextGuid();
            Guid mappedId = NewId.NextGuid();
            Guid conventionalId = NewId.NextGuid();

            await harness.InputQueueSendEndpoint.SendAsync(new CorrelatedStart(correlatedId), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new MappedStart(mappedId), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new ConventionalStart(conventionalId), cancellationToken);

            Assert.Equal(correlatedId, await sagaHarness.WaitForSagaInStateAsync(correlatedId, machine.Running, timeout, TestContext.Current.CancellationToken));
            Assert.Equal(mappedId, await sagaHarness.WaitForSagaInStateAsync(mappedId, machine.Running, timeout, TestContext.Current.CancellationToken));
            Assert.Equal(conventionalId, await sagaHarness.WaitForSagaInStateAsync(conventionalId, machine.Running, timeout, TestContext.Current.CancellationToken));

            await harness.InputQueueSendEndpoint.SendAsync(new CorrelatedStop(correlatedId), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new MappedStop(mappedId), cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new ConventionalStop(conventionalId), cancellationToken);

            Assert.Equal(correlatedId, await sagaHarness.WaitForSagaInStateAsync(correlatedId, machine.Final, timeout, TestContext.Current.CancellationToken));
            Assert.Equal(mappedId, await sagaHarness.WaitForSagaInStateAsync(mappedId, machine.Final, timeout, TestContext.Current.CancellationToken));
            Assert.Equal(conventionalId, await sagaHarness.WaitForSagaInStateAsync(conventionalId, machine.Final, timeout, TestContext.Current.CancellationToken));

            AssertInstance(sagaHarness.Sagas.FindById(correlatedId), CorrelationPath.Correlated, machine.Final.Name);
            AssertInstance(sagaHarness.Sagas.FindById(mappedId), CorrelationPath.MessageTopology, machine.Final.Name);
            AssertInstance(sagaHarness.Sagas.FindById(conventionalId), CorrelationPath.PropertyConvention, machine.Final.Name);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Consumed.Snapshot<CorrelatedStart>());
        Assert.Single(harness.Consumed.Snapshot<CorrelatedStop>());
        Assert.Single(harness.Consumed.Snapshot<MappedStart>());
        Assert.Single(harness.Consumed.Snapshot<MappedStop>());
        Assert.Single(harness.Consumed.Snapshot<ConventionalStart>());
        Assert.Single(harness.Consumed.Snapshot<ConventionalStop>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-CORRELATION", "business-key-select-id-insert-and-factory")]
    public async Task BusinessKeyCorrelation_UsesTheFactoryAndFindsTheSameInstanceThroughFinalAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("business-key", timeout);
        var machine = new BusinessKeyMachine();
        ISagaStateMachineTestHarness<BusinessKeyMachine, BusinessKeyState> sagaHarness =
            harness.AddSagaStateMachine<BusinessKeyMachine, BusinessKeyState>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid transactionId = NewId.NextGuid();

            await harness.Bus.PublishAsync(new BeginTransaction(transactionId), cancellationToken);
            IReadOnlyList<Guid> active = await sagaHarness.WaitForSagasInStateAsync(instance => instance.TransactionId == transactionId, machine.Active, timeout, TestContext.Current.CancellationToken);
            BusinessKeyState? instance = sagaHarness.Sagas.FindById(Assert.Single(active));

            Assert.NotNull(instance);
            Assert.Equal(transactionId, instance.CorrelationId);
            Assert.Equal(transactionId, instance.TransactionId);
            Assert.Equal("factory", instance.CreatedBy);
            Assert.Equal(1, instance.BeginCount);

            await harness.Bus.PublishAsync(new CommitTransaction(transactionId), cancellationToken);
            IReadOnlyList<Guid> final = await sagaHarness.WaitForSagasInStateAsync(candidate => candidate.TransactionId == transactionId, machine.Final, timeout, TestContext.Current.CancellationToken);

            Assert.Equal(active, final);
            Assert.Equal(machine.Final.Name, instance.CurrentState);
            Assert.Equal(1, instance.CommitCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-LIFECYCLE", "running-final-and-initial-response-removal")]
    public async Task CompletedInstance_IsRemovedAfterBothRunningAndInitialFinalizationPathsAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("completed-removal", timeout);
        var machine = new RemovingMachine();
        var repository = new InMemorySagaRepository<RemovingState>();
        ISagaStateMachineTestHarness<RemovingMachine, RemovingState> sagaHarness =
            harness.AddSagaStateMachine<RemovingMachine, RemovingState>(machine, repository);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid runningId = NewId.NextGuid();
            await harness.InputQueueSendEndpoint.SendAsync(new RemovalStart(runningId), cancellationToken);
            Assert.Equal(runningId, await sagaHarness.WaitForSagaInStateAsync(runningId, machine.Running, timeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, Assert.IsType<RemovingState>(sagaHarness.Sagas.FindById(runningId)).StartCount);

            Task<IConsumedMessage<RemovalStop>> stop = harness.Consumed
                .SelectAsync<RemovalStop>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(new RemovalStop(runningId), cancellationToken);
            Assert.Null((await stop.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Null(await repository.LoadAsync(runningId, TestContext.Current.CancellationToken));

            Guid immediateId = NewId.NextGuid();
            Task<IConsumedMessage<ImmediateRemovalRequest>> immediate = harness.Consumed
                .SelectAsync<ImmediateRemovalRequest>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            IRequestClient<ImmediateRemovalRequest> client = harness.CreateRequestClient<ImmediateRemovalRequest>();
            Response<ImmediateRemovalResponse> response = await client.GetResponseAsync<ImmediateRemovalResponse>(
                new ImmediateRemovalRequest(immediateId),
                cancellationToken);

            Assert.Equal(immediateId, response.Message.CorrelationId);
            Assert.Equal("removed-from-initial", response.Message.Result);
            Assert.Null((await immediate.WaitAsync(timeout, cancellationToken)).Exception);
            Assert.Null(await repository.LoadAsync(immediateId, TestContext.Current.CancellationToken));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static void AssertInstance(ConventionState? instance, CorrelationPath path, string expectedState)
    {
        Assert.NotNull(instance);
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


    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum CorrelationPath
    {
        Correlated,
        MessageTopology,
        PropertyConvention,
    }

    public sealed record CorrelatedStart(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record CorrelatedStop(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record MappedStart(Guid ServiceId);

    public sealed record MappedStop(Guid ServiceId);

    public sealed record ConventionalStart(Guid CorrelationId);

    public sealed record ConventionalStop(Guid CorrelationId);

    public sealed class ConventionState : ISagaStateMachineInstance
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

        public IState Running { get; } = null!;

        public IEvent<CorrelatedStart> CorrelatedStarted { get; } = null!;

        public IEvent<CorrelatedStop> CorrelatedStopped { get; } = null!;

        public IEvent<MappedStart> MappedStarted { get; } = null!;

        public IEvent<MappedStop> MappedStopped { get; } = null!;

        public IEvent<ConventionalStart> ConventionalStarted { get; } = null!;

        public IEvent<ConventionalStop> ConventionalStopped { get; } = null!;
    }

    public sealed record BeginTransaction(Guid TransactionId);

    public sealed record CommitTransaction(Guid TransactionId);

    public sealed class BusinessKeyState : ISagaStateMachineInstance
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

        public IState Active { get; } = null!;

        public IEvent<BeginTransaction> Begin { get; } = null!;

        public IEvent<CommitTransaction> Commit { get; } = null!;
    }

    public sealed record RemovalStart(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record RemovalStop(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record ImmediateRemovalRequest(Guid CorrelationId) : ICorrelatedBy<Guid>;

    public sealed record ImmediateRemovalResponse(Guid CorrelationId, string Result) : ICorrelatedBy<Guid>;

    public sealed class RemovingState : ISagaStateMachineInstance
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

        public IState Running { get; } = null!;

        public IEvent<RemovalStart> Start { get; } = null!;

        public IEvent<RemovalStop> Stop { get; } = null!;

        public IEvent<ImmediateRemovalRequest> Immediate { get; } = null!;
    }
}
