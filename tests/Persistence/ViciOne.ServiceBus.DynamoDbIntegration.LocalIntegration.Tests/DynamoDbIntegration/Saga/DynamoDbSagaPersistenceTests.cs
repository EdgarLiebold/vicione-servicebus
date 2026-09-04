using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DynamoDbIntegration.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.DynamoDbIntegration.Saga;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.DynamoDbIntegration.LocalIntegration.Tests.DynamoDbIntegration.Saga;

public sealed class DynamoDbSagaPersistenceTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-PERSISTENCE", "correlated-message-loads-only-its-saga")]
    public async Task CorrelatedMessage_LoadsOnlyItsSaga()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DynamoDbTestTable fixture = await DynamoDbTestTable.CreateAsync("SagaCorrelation", cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(fixture.OperationTimeout, cancellationToken);

        try
        {
            Guid firstId = Guid.NewGuid();
            Guid secondId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpoint<PersistentSaga>();

            await endpoint.Send(new StartPersistentSaga(firstId, "first"), cancellationToken);
            await endpoint.Send(new StartPersistentSaga(secondId, "second"), cancellationToken);
            await harness.Published.SelectAsync<PersistentSagaStarted>(
                    observed => observed.Context.Message.CorrelationId == firstId,
                    cancellationToken)
                .First();
            await harness.Published.SelectAsync<PersistentSagaStarted>(
                    observed => observed.Context.Message.CorrelationId == secondId,
                    cancellationToken)
                .First();

            await endpoint.Send(new MovePersistentSaga(firstId), cancellationToken);
            IPublishedMessage<PersistentSagaMoved> moved = await harness.Published
                .SelectAsync<PersistentSagaMoved>(
                    observed => observed.Context.Message.CorrelationId == firstId,
                    cancellationToken)
                .First();

            var repository = (ILoadSagaRepository<PersistentSaga>)DynamoDbSagaRepository<PersistentSaga>
                .Create(fixture.CreateContext, fixture.TableName);
            PersistentSaga first = await repository.Load(firstId);
            PersistentSaga second = await repository.Load(secondId);

            Assert.Equal(firstId, moved.Context.Message.CorrelationId);
            Assert.True(first.Moved);
            Assert.Equal("first", first.Name);
            Assert.False(second.Moved);
            Assert.Equal("second", second.Name);
            Assert.NotEqual(first.CorrelationId, second.CorrelationId);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-PERSISTENCE", "initiating-message-creates-one-persisted-saga")]
    public async Task InitiatingMessage_CreatesOnePersistedSaga()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DynamoDbTestTable fixture = await DynamoDbTestTable.CreateAsync("SagaInitiation", cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture);
        ITestHarness harness = await provider.StartTestHarness().WaitAsync(fixture.OperationTimeout, cancellationToken);

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

            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>[] rows = await fixture.ScanAsync(cancellationToken);
            var repository = (ILoadSagaRepository<PersistentSaga>)DynamoDbSagaRepository<PersistentSaga>
                .Create(fixture.CreateContext, fixture.TableName);
            PersistentSaga persisted = await repository.Load(sagaId);

            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> row = Assert.Single(rows);
            Assert.Equal(sagaId.ToString("D"), row["PK"].S);
            Assert.Equal(DynamoDbSaga.DefaultEntityType, row["SK"].S);
            Assert.Equal(sagaId, started.Context.Message.CorrelationId);
            Assert.Equal(sagaId, persisted.CorrelationId);
            Assert.Equal("created", persisted.Name);
            Assert.Equal(0, persisted.Version);
        }
        finally
        {
            await harness.Stop(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static ServiceProvider CreateProvider(DynamoDbTestTable fixture) => new ServiceCollection()
        .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            configuration.SetTestTimeouts(fixture.OperationTimeout, fixture.OperationTimeout);
            configuration.AddSaga<PersistentSaga, PersistentSagaDefinition>()
                .DynamoDbRepository(repository =>
                {
                    repository.TableName = fixture.TableName;
                    repository.ContextFactory(fixture.CreateContext);
                });
        })
        .BuildServiceProvider(validateScopes: true);

    public sealed record StartPersistentSaga(Guid CorrelationId, string Name) : CorrelatedBy<Guid>;

    public sealed record MovePersistentSaga(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record PersistentSagaStarted(Guid CorrelationId);

    public sealed record PersistentSagaMoved(Guid CorrelationId);

    public sealed class PersistentSaga :
        ISagaVersion,
        InitiatedBy<StartPersistentSaga>,
        Orchestrates<MovePersistentSaga>
    {
        public Guid CorrelationId { get; set; }

        public int Version { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool Moved { get; set; }

        public Task Consume(ConsumeContext<StartPersistentSaga> context)
        {
            Name = context.Message.Name;
            return context.Publish(new PersistentSagaStarted(CorrelationId), context.CancellationToken);
        }

        public Task Consume(ConsumeContext<MovePersistentSaga> context)
        {
            Moved = true;
            return context.Publish(new PersistentSagaMoved(CorrelationId), context.CancellationToken);
        }
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
}
