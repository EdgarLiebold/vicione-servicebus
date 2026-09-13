using System.Linq.Expressions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas.Configuration;

public sealed class SagaConnectorTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONTRACT-DISCOVERY", "semantic-role-order-and-stable-message-order")]
    public void MessageContracts_AreDiscoveredInSemanticRoleAndStableMessageOrder()
    {
        Type[] messageTypes = new SagaConnector<OrderedSaga>().Connectors
            .Select(connector => connector.MessageType)
            .ToArray();

        Assert.Equal(
            [
                typeof(AlphaInitiated),
                typeof(ZuluInitiated),
                typeof(AlphaOrchestrated),
                typeof(ZuluOrchestrated),
                typeof(CombinedMessage),
                typeof(ObservedMessage),
            ],
            messageTypes);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DUPLICATE-ROLE-INSTANCE", "initiated-by-precedence-and-guid-constructor")]
    public async Task DuplicateInitiatedAndOrchestratedRole_CreatesTheMissingSagaOnceAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid correlationId = NewId.NextGuid();
        using var harness = CreateHarness(timeout);
        SagaTestHarness<DuplicateRoleSaga> sagaHarness = harness.AddSaga<DuplicateRoleSaga>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new DuplicateRoleMessage(correlationId), cancellationToken);

            Assert.True(await sagaHarness.Consumed.AnyAsync<DuplicateRoleMessage>(cancellationToken));
            Guid? createdId = await sagaHarness.WaitForSagaAsync(correlationId, timeout, TestContext.Current.CancellationToken);
            DuplicateRoleSaga? created = sagaHarness.Created.FindById(correlationId);

            Assert.Equal(correlationId, createdId);
            Assert.NotNull(created);
            Assert.Equal(correlationId, created.CorrelationId);
            Assert.Equal(1, created.ConsumeCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "parameterless-constructor-and-writable-correlation-id")]
    public async Task WritableCorrelationIdSaga_ReceivesTheMessageCorrelationIdBeforeConsumptionAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid correlationId = NewId.NextGuid();
        using var harness = CreateHarness(timeout);
        SagaTestHarness<PropertySaga> sagaHarness = harness.AddSaga<PropertySaga>();

        await harness.StartAsync(cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(new PropertySagaMessage(correlationId), cancellationToken);

            Assert.True(await sagaHarness.Consumed.AnyAsync<PropertySagaMessage>(cancellationToken));
            Guid? createdId = await sagaHarness.WaitForSagaAsync(correlationId, timeout, TestContext.Current.CancellationToken);
            PropertySaga? created = sagaHarness.Created.FindById(correlationId);

            Assert.Equal(correlationId, createdId);
            Assert.NotNull(created);
            Assert.Equal(correlationId, created.CorrelationId);
            Assert.Equal(correlationId, created.CorrelationIdObservedDuringConsume);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONTRACT-VALIDATION", "missing-supported-message-contract")]
    public void SagaWithoutSupportedMessageContract_FailsWithOneActionableConfigurationError()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => new SagaConnector<ContractlessSaga>());

        Assert.Null(exception.InnerException);
        Assert.Contains(nameof(ContractlessSaga), exception.Message, StringComparison.Ordinal);
        Assert.Contains("does not declare a supported saga message contract", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-CONTRACT-VALIDATION", "unsupported-message-contract-is-filtered")]
    public void SagaWithOnlyAnUnsupportedMessageContract_FailsAsContractless()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => new SagaConnector<UnsupportedMessageSaga>());

        Assert.Null(exception.InnerException);
        Assert.Contains(nameof(UnsupportedMessageSaga), exception.Message, StringComparison.Ordinal);
        Assert.Contains("does not declare a supported saga message contract", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INSTANCE-FACTORY", "unsupported-constructor-shape")]
    public void SagaWithoutASupportedConstructionShape_FailsWithOneActionableConfigurationError()
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => new SagaConnector<UnsupportedConstructorSaga>());

        Assert.Null(exception.InnerException);
        Assert.Contains(nameof(UnsupportedConstructorSaga), exception.Message, StringComparison.Ordinal);
        Assert.Contains("public constructor with one Guid parameter", exception.Message, StringComparison.Ordinal);
        Assert.Contains("public parameterless constructor", exception.Message, StringComparison.Ordinal);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"saga-connector-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    public sealed record AlphaInitiated(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ZuluInitiated(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record AlphaOrchestrated(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ZuluOrchestrated(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record CombinedMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record ObservedMessage(string Key);

    public sealed class OrderedSaga :
        ISaga,
        InitiatedBy<ZuluInitiated>,
        InitiatedBy<AlphaInitiated>,
        Orchestrates<ZuluOrchestrated>,
        Orchestrates<AlphaOrchestrated>,
        InitiatedByOrOrchestrates<CombinedMessage>,
        Observes<ObservedMessage, OrderedSaga>
    {
        public OrderedSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public string Key { get; private set; } = string.Empty;

        public Expression<Func<OrderedSaga, ObservedMessage, bool>> CorrelationExpression =>
            (saga, message) => saga.Key == message.Key;

        public Task ConsumeAsync(ConsumeContext<AlphaInitiated> context) => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<ZuluInitiated> context) => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<AlphaOrchestrated> context) => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<ZuluOrchestrated> context) => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<CombinedMessage> context) => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<ObservedMessage> context)
        {
            Key = context.Message.Key;
            return Task.CompletedTask;
        }
    }

    public sealed record DuplicateRoleMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class DuplicateRoleSaga :
        ISaga,
        InitiatedBy<DuplicateRoleMessage>,
        Orchestrates<DuplicateRoleMessage>
    {
        public DuplicateRoleSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public int ConsumeCount { get; private set; }

        public Task ConsumeAsync(ConsumeContext<DuplicateRoleMessage> context)
        {
            ConsumeCount++;
            return Task.CompletedTask;
        }
    }

    public sealed record PropertySagaMessage(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class PropertySaga :
        ISaga,
        InitiatedBy<PropertySagaMessage>
    {
        public Guid CorrelationId { get; set; }

        public Guid CorrelationIdObservedDuringConsume { get; private set; }

        public Task ConsumeAsync(ConsumeContext<PropertySagaMessage> context)
        {
            CorrelationIdObservedDuringConsume = CorrelationId;
            return Task.CompletedTask;
        }
    }

    public sealed class ContractlessSaga : ISaga
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed class UnsupportedMessageSaga : ISaga, InitiatedBy<CorrelatedBy<Guid>>
    {
        public UnsupportedMessageSaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<CorrelatedBy<Guid>> context) => Task.CompletedTask;
    }

    public sealed class UnsupportedConstructorSaga : ISaga, InitiatedBy<PropertySagaMessage>
    {
        public UnsupportedConstructorSaga(string correlationId)
        {
            CorrelationId = Guid.Parse(correlationId);
        }

        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<PropertySagaMessage> context) => Task.CompletedTask;
    }
}
