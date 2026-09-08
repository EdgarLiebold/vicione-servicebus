using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DynamoDb;
using ViciOne.ServiceBus.DynamoDb.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.DynamoDb.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Sagas.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.DynamoDb.LocalIntegration.Tests.Saga;

public sealed class DynamoDbSagaPersistenceTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-PERSISTENCE", "correlated-message-loads-only-its-saga")]
    public async Task CorrelatedMessage_LoadsOnlyItsSagaAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DynamoDbTestTable fixture = await DynamoDbTestTable.CreateAsync("SagaCorrelation", cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

        try
        {
            Guid firstId = Guid.NewGuid();
            Guid secondId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<PersistentSaga>(TestContext.Current.CancellationToken);

            await endpoint.SendAsync(new StartPersistentSaga(firstId, "first"), cancellationToken);
            await endpoint.SendAsync(new StartPersistentSaga(secondId, "second"), cancellationToken);
            await harness.Published.SelectAsync<PersistentSagaStarted>(
                    observed => observed.Context.Message.CorrelationId == firstId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await harness.Published.SelectAsync<PersistentSagaStarted>(
                    observed => observed.Context.Message.CorrelationId == secondId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await endpoint.SendAsync(new MovePersistentSaga(firstId), cancellationToken);
            IPublishedMessage<PersistentSagaMoved> moved = await harness.Published
                .SelectAsync<PersistentSagaMoved>(
                    observed => observed.Context.Message.CorrelationId == firstId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            var repository = (ILoadSagaRepository<PersistentSaga>)DynamoDbSagaRepository.Create(
                fixture.CreateContext,
                new DynamoDbSagaRepositoryOptions<PersistentSaga>(fixture.TableName));
            PersistentSaga first = Assert.IsType<PersistentSaga>(
                await repository.LoadAsync(firstId, TestContext.Current.CancellationToken));
            PersistentSaga second = Assert.IsType<PersistentSaga>(
                await repository.LoadAsync(secondId, TestContext.Current.CancellationToken));

            Assert.Equal(firstId, moved.Context.Message.CorrelationId);
            Assert.True(first.Moved);
            Assert.Equal("first", first.Name);
            Assert.False(second.Moved);
            Assert.Equal("second", second.Name);
            Assert.NotEqual(first.CorrelationId, second.CorrelationId);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-DYNAMODB-SAGA-PERSISTENCE", "initiating-message-creates-one-persisted-saga")]
    public async Task InitiatingMessage_CreatesOnePersistedSagaAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using DynamoDbTestTable fixture = await DynamoDbTestTable.CreateAsync("SagaInitiation", cancellationToken);
        await using ServiceProvider provider = CreateProvider(fixture);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

        try
        {
            Guid sagaId = Guid.NewGuid();
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<PersistentSaga>(TestContext.Current.CancellationToken);
            await endpoint.SendAsync(new StartPersistentSaga(sagaId, "created"), cancellationToken);
            IPublishedMessage<PersistentSagaStarted> started = await harness.Published
                .SelectAsync<PersistentSagaStarted>(
                    observed => observed.Context.Message.CorrelationId == sagaId,
                    cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>[] rows = await fixture.ScanAsync(cancellationToken);
            var repository = (ILoadSagaRepository<PersistentSaga>)DynamoDbSagaRepository.Create(
                fixture.CreateContext,
                new DynamoDbSagaRepositoryOptions<PersistentSaga>(fixture.TableName));
            PersistentSaga persisted = Assert.IsType<PersistentSaga>(
                await repository.LoadAsync(sagaId, TestContext.Current.CancellationToken));

            Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue> row = Assert.Single(rows);
            Assert.Equal(sagaId.ToString("D"), row["PK"].S);
            Assert.Equal(DynamoDbSagaDocument.EntityTypeValue, row["SK"].S);
            Assert.Equal(sagaId, started.Context.Message.CorrelationId);
            Assert.Equal(sagaId, persisted.CorrelationId);
            Assert.Equal("created", persisted.Name);
            Assert.Equal(0, persisted.Version);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static ServiceProvider CreateProvider(DynamoDbTestTable fixture) => new ServiceCollection()
        .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
        {
            configuration.SetTestTimeouts(fixture.OperationTimeout, fixture.OperationTimeout);
            configuration.AddSaga<PersistentSaga, PersistentSagaDefinition>()
                .UseDynamoDb(repository =>
                {
                    repository.TableName = fixture.TableName;
                    repository.UseContextFactory(fixture.CreateContext);
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

        public Task ConsumeAsync(ConsumeContext<StartPersistentSaga> context)
        {
            Name = context.Message.Name;
            return context.Advanced().PublishAsync(new PersistentSagaStarted(CorrelationId), context.CancellationToken);
        }

        public Task ConsumeAsync(ConsumeContext<MovePersistentSaga> context)
        {
            Moved = true;
            return context.Advanced().PublishAsync(new PersistentSagaMoved(CorrelationId), context.CancellationToken);
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
            sagaConfigurator.UseVolatileOutbox(context);
        }
    }
}
