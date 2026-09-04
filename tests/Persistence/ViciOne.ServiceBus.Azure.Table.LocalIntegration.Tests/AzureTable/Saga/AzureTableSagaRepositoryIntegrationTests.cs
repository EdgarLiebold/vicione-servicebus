using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.AzureTable.Saga;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.AzureTable.Saga;

public sealed class AzureTableSagaRepositoryIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-LIFECYCLE", "initiate-correlate-update-and-reload-on-real-table-api")]
    public async Task Repository_PersistsAndReloadsTheCorrelatedSagaLifecycle()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = OperationTimeout();
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("SagaLifecycle", cancellationToken);
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSaga<PersistentSaga, PersistentSagaDefinition>()
                    .AzureTableRepository(repository => repository.TableClientFactory(() => fixture.Table));
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpoint<PersistentSaga>();

            await endpoint.Send(new StartPersistentSaga(sagaId, "created"), cancellationToken);
            IPublishedMessage<PersistentSagaStarted> started = await harness.Published
                .SelectAsync<PersistentSagaStarted>(
                    observed => observed.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .First();
            await endpoint.Send(new UpdatePersistentSaga(sagaId, "updated"), cancellationToken);
            IPublishedMessage<PersistentSagaUpdated> updated = await harness.Published
                .SelectAsync<PersistentSagaUpdated>(
                    observed => observed.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .First();

            var repository = (ILoadSagaRepository<PersistentSaga>)AzureTableSagaRepository<PersistentSaga>
                .Create(() => fixture.Table);
            PersistentSaga persisted = await repository.Load(sagaId);

            Assert.Equal("created", started.Context.Message.Value);
            Assert.Equal("updated", updated.Context.Message.Value);
            Assert.Equal(sagaId, persisted.CorrelationId);
            Assert.Equal("updated", persisted.Value);
            Assert.Equal(2, persisted.Revision);
            Assert.Equal(PersistentSagaStage.Updated, persisted.Stage);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-READ-ONLY", "response-observes-state-without-persisting-handler-mutation")]
    public async Task ReadOnlyEvent_RespondsFromPersistedStateWithoutSavingItsMutation()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TimeSpan timeout = OperationTimeout();
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("SagaReadOnly", cancellationToken);
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSagaStateMachine<ReadOnlyStateMachine, ReadOnlyState, ReadOnlyStateDefinition>()
                    .AzureTableRepository(repository => repository.TableClientFactory(() => fixture.Table));
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(timeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            IRequestClient<StartReadOnlySaga> startClient = harness.GetRequestClient<StartReadOnlySaga>();
            Response<ReadOnlySagaStarted> started = await startClient.GetResponse<ReadOnlySagaStarted>(
                new StartReadOnlySaga(sagaId),
                cancellationToken);
            IRequestClient<CheckReadOnlySaga> statusClient = harness.GetRequestClient<CheckReadOnlySaga>();

            Response<ReadOnlySagaStatus> first = await statusClient.GetResponse<ReadOnlySagaStatus>(
                new CheckReadOnlySaga(sagaId),
                cancellationToken);
            Response<ReadOnlySagaStatus> second = await statusClient.GetResponse<ReadOnlySagaStatus>(
                new CheckReadOnlySaga(sagaId),
                cancellationToken);
            var repository = (ILoadSagaRepository<ReadOnlyState>)AzureTableSagaRepository<ReadOnlyState>
                .Create(() => fixture.Table);
            ReadOnlyState persisted = await repository.Load(sagaId);

            Assert.Equal(sagaId, started.Message.CorrelationId);
            Assert.Equal("Started", first.Message.Status);
            Assert.Equal("Started", second.Message.Status);
            Assert.Equal("Started", persisted.Status);
            Assert.Equal(ReadOnlyStateMachine.RunningStateName, persisted.CurrentState);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed record StartPersistentSaga(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record UpdatePersistentSaga(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    public sealed record PersistentSagaStarted(Guid CorrelationId, string Value);

    public sealed record PersistentSagaUpdated(Guid CorrelationId, string Value);

    public sealed class PersistentSaga :
        ISaga,
        InitiatedBy<StartPersistentSaga>,
        Orchestrates<UpdatePersistentSaga>
    {
        public Guid CorrelationId { get; set; }

        public int Revision { get; set; }

        public PersistentSagaStage Stage { get; set; }

        public string Value { get; set; } = string.Empty;

        public Task Consume(ConsumeContext<StartPersistentSaga> context)
        {
            Value = context.Message.Value;
            Revision = 1;
            Stage = PersistentSagaStage.Started;
            return context.Publish(new PersistentSagaStarted(CorrelationId, Value), context.CancellationToken);
        }

        public Task Consume(ConsumeContext<UpdatePersistentSaga> context)
        {
            Value = context.Message.Value;
            Revision++;
            Stage = PersistentSagaStage.Updated;
            return context.Publish(new PersistentSagaUpdated(CorrelationId, Value), context.CancellationToken);
        }
    }

    public enum PersistentSagaStage
    {
        Initial = 0,
        Started = 1,
        Updated = 2,
    }

    private sealed class PersistentSagaDefinition : SagaDefinition<PersistentSaga>
    {
        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<PersistentSaga> sagaConfigurator,
            IRegistrationContext context)
        {
            sagaConfigurator.UseMessageRetry(retry => retry.Immediate(2));
            sagaConfigurator.UseInMemoryOutbox(context);
        }
    }

    public sealed record StartReadOnlySaga(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record CheckReadOnlySaga(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ReadOnlySagaStarted(Guid CorrelationId);

    public sealed record ReadOnlySagaStatus(Guid CorrelationId, string Status);

    public sealed class ReadOnlyState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }

    public sealed class ReadOnlyStateMachine : ViciOneServiceBusStateMachine<ReadOnlyState>
    {
        public const string RunningStateName = "Running";

        public ReadOnlyStateMachine()
        {
            InstanceState(state => state.CurrentState);
            Event(() => Started, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            Event(() => StatusRequested, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.ReadOnly = true;
            });

            Initially(
                When(Started)
                    .Then(context => context.Saga.Status = "Started")
                    .Respond(context => new ReadOnlySagaStarted(context.Saga.CorrelationId))
                    .TransitionTo(Running));
            During(Running,
                When(StatusRequested)
                    .Respond(context => new ReadOnlySagaStatus(context.Saga.CorrelationId, context.Saga.Status))
                    .Then(context => context.Saga.Status = "This mutation must not be persisted"));
        }

        public State Running { get; private set; } = null!;

        public Event<StartReadOnlySaga> Started { get; private set; } = null!;

        public Event<CheckReadOnlySaga> StatusRequested { get; private set; } = null!;
    }

    private sealed class ReadOnlyStateDefinition : SagaDefinition<ReadOnlyState>
    {
        protected override void ConfigureSaga(
            IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<ReadOnlyState> sagaConfigurator,
            IRegistrationContext context) => sagaConfigurator.UseInMemoryOutbox(context);
    }
}
