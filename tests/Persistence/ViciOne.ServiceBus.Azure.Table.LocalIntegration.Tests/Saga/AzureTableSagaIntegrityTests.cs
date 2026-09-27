using global::Azure;
using global::Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Azure.Table.Saga;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Sagas.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Saga;

public sealed class AzureTableSagaIntegrityTests
{
    [Theory]
    [InlineData("Count")]
    [InlineData("Owner")]
    [InlineData("Enabled")]
    [InlineData("Amount")]
    [InlineData("Stage")]
    [InlineData("Detail")]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-INTEGRITY", "corruption-faults-before-consumer-and-repaired-row-recovers")]
    public async Task CorruptedRow_FaultsBeforeConsumerAndRecoversAfterRepairAsync(string property)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("CorruptSaga", token);
        var storage = new AzureTableSagaStorageContext<IntegrityState>(
            fixture.Table, new FixedPartitionSagaKeyFormatter(nameof(IntegrityState)));
        var original = new IntegrityState { CorrelationId = Guid.NewGuid(), Owner = Guid.NewGuid() };
        var neighbor = new IntegrityState { CorrelationId = Guid.NewGuid(), Count = 83, Owner = Guid.NewGuid() };
        var row = new TableEntity(storage.Converter.GetDictionary(original));
        (row.PartitionKey, row.RowKey) = storage.Format(original.CorrelationId);
        string storageName = $"Saga_{property}";
        object correctValue = row[storageName];
        row[storageName] = property switch
        {
            "Count" => (long)original.Count,
            "Owner" => original.Owner.ToString("D"),
            "Enabled" => "true",
            "Amount" => "{\"amount\":",
            "Stage" => "\"no-such-stage\"",
            "Detail" => "{\"label\":",
            _ => throw new ArgumentOutOfRangeException(nameof(property)),
        };
        await fixture.Table.AddEntityAsync(row, token);
        var neighborRow = new TableEntity(storage.Converter.GetDictionary(neighbor));
        (neighborRow.PartitionKey, neighborRow.RowKey) = storage.Format(neighbor.CorrelationId);
        await fixture.Table.AddEntityAsync(neighborRow, token);
        TableEntity before = (await fixture.Table.GetEntityAsync<TableEntity>(row.PartitionKey, row.RowKey, cancellationToken: token)).Value;
        TableEntity neighborBefore = (await fixture.Table.GetEntityAsync<TableEntity>(neighborRow.PartitionKey, neighborRow.RowKey, cancellationToken: token)).Value;
        var repository = (ILoadSagaRepository<IntegrityState>)AzureTableSagaRepository.Create<IntegrityState>(() => fixture.Table);
        Exception loadFailure = Assert.IsAssignableFrom<Exception>(await Record.ExceptionAsync(async () =>
            await repository.LoadAsync(original.CorrelationId, TestContext.Current.CancellationToken)));
        if (property is "Count" or "Owner" or "Enabled")
        {
            Assert.IsType<InvalidOperationException>(loadFailure);
            Assert.Contains(storageName, loadFailure.Message, StringComparison.Ordinal);
        }
        else
            Assert.IsAssignableFrom<System.Text.Json.JsonException>(loadFailure);

        var probe = new HandlerProbe();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(probe)
            .AddViciOneServiceBusTestHarness(TextWriter.Null, configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddSagaStateMachine<IntegrityMachine, IntegrityState, IntegrityDefinition>()
                    .UseAzureTable(repositoryConfiguration => repositoryConfiguration.UseTableClientFactory(() => fixture.Table));
            })
            .BuildServiceProvider(validateScopes: true);
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: token).WaitAsync(timeout, token);
        try
        {
            ISendEndpoint endpoint = await harness.GetSagaEndpointAsync<IntegrityState>(token);
            var rejected = new ApplyChange(original.CorrelationId, Guid.NewGuid());
            await endpoint.SendAsync(rejected, token);
            IPublishedMessage<Fault<ApplyChange>> fault = await harness.Published
                .SelectAsync<Fault<ApplyChange>>(observed => observed.Context.Message.Message.CommandId == rejected.CommandId, token)
                .FirstObservedAsync(cancellationToken: token);
            Assert.Equal(original.CorrelationId, fault.Context.Message.Message.CorrelationId);
            Assert.Contains(fault.Context.Message.Exceptions, info => ContainsFailure(info, loadFailure.GetType().FullName!));
            Assert.Equal(0, probe.Count);
            Assert.Empty(harness.Published.Snapshot<ChangeApplied>());
            AssertSameRow(before, (await fixture.Table.GetEntityAsync<TableEntity>(row.PartitionKey, row.RowKey, cancellationToken: token)).Value);
            AssertSameRow(neighborBefore, (await fixture.Table.GetEntityAsync<TableEntity>(neighborRow.PartitionKey, neighborRow.RowKey, cancellationToken: token)).Value);

            var replacement = new TableEntity(storage.Converter.GetDictionary(original))
            {
                PartitionKey = row.PartitionKey,
                RowKey = row.RowKey,
            };
            await fixture.Table.UpdateEntityAsync(replacement, before.ETag, TableUpdateMode.Replace, token);
            TableEntity repaired = (await fixture.Table.GetEntityAsync<TableEntity>(row.PartitionKey, row.RowKey, cancellationToken: token)).Value;
            Assert.Equal(correctValue.GetType(), repaired[storageName].GetType());
            Assert.Equal(correctValue, repaired[storageName]);
            IntegrityState repairedState = Assert.IsType<IntegrityState>(await repository.LoadAsync(original.CorrelationId, token));
            Assert.Equal(original.Count, repairedState.Count);
            var accepted = new ApplyChange(original.CorrelationId, Guid.NewGuid());
            await endpoint.SendAsync(accepted, token);
            IPublishedMessage<ChangeApplied> success = await harness.Published
                .SelectAsync<ChangeApplied>(observed => observed.Context.Message.CommandId == accepted.CommandId, token)
                .FirstObservedAsync(cancellationToken: token);
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);

            IntegrityState persisted = Assert.IsType<IntegrityState>(await repository.LoadAsync(original.CorrelationId, token));
            Assert.Equal(original.Count + 1, persisted.Count);
            Assert.Equal(original.Owner, persisted.Owner);
            Assert.Equal(original.Enabled, persisted.Enabled);
            Assert.Equal(original.Amount, persisted.Amount);
            Assert.Equal(original.Stage, persisted.Stage);
            Assert.Equal(original.Detail.Label, persisted.Detail.Label);
            Assert.Equal(1, probe.Count);
            Assert.Equal(original.CorrelationId, success.Context.Message.CorrelationId);
            Assert.Equal(persisted.Count, success.Context.Message.Count);
            Assert.Equal(accepted.CommandId, Assert.Single(harness.Published.Snapshot<ChangeApplied>()).Context.Message.CommandId);
            Assert.Single(harness.Published.Snapshot<Fault<ApplyChange>>());
            AssertSameRow(neighborBefore, (await fixture.Table.GetEntityAsync<TableEntity>(neighborRow.PartitionKey, neighborRow.RowKey, cancellationToken: token)).Value);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-SCHEMA", "missing-defaults-and-explicit-empty-values-survive-update")]
    public async Task LegacyRow_PreservesDefaultsAndExplicitEmptyValuesAcrossUpdateAsync(bool explicitValues)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("LegacySaga", token);
        var storage = new AzureTableSagaStorageContext<EvolvingState>(fixture.Table, new FixedPartitionSagaKeyFormatter(nameof(EvolvingState)));
        var state = new EvolvingState { CorrelationId = Guid.NewGuid() };
        var row = new TableEntity(storage.Converter.GetDictionary(state));
        (row.PartitionKey, row.RowKey) = storage.Format(state.CorrelationId);
        foreach (string property in new[] { "Count", "Enabled", "Text", "Bytes", "Detail" })
            row.Remove($"Saga_{property}");
        if (explicitValues)
        {
            row["Saga_Count"] = 0;
            row["Saga_Enabled"] = false;
            row["Saga_Text"] = string.Empty;
            row["Saga_Bytes"] = Array.Empty<byte>();
            row["Saga_Detail"] = "null";
        }
        await fixture.Table.AddEntityAsync(row, token);
        ConsumeContext<ApplyChange> consumeContext = InMemoryOutboxTestContextFactory.Create(new ApplyChange(state.CorrelationId, Guid.NewGuid()), token);
        var repository = new AzureTableSagaRepositoryContext<EvolvingState, ApplyChange>(storage, consumeContext,
            new SagaConsumeContextFactory<IAzureTableSagaStorageContext<EvolvingState>, EvolvingState>());
        SagaConsumeContext<EvolvingState, ApplyChange> loaded = Assert.IsAssignableFrom<SagaConsumeContext<EvolvingState, ApplyChange>>(
            await repository.LoadAsync(state.CorrelationId, token));
        Assert.Equal(explicitValues ? 0 : 7, loaded.Saga.Count);
        Assert.Equal(!explicitValues, loaded.Saga.Enabled);
        Assert.Equal(explicitValues ? string.Empty : "default", loaded.Saga.Text);
        Assert.Equal(explicitValues ? Array.Empty<byte>() : new byte[] { 9 }, loaded.Saga.Bytes);
        if (explicitValues)
            Assert.Null(loaded.Saga.Detail);
        else
            Assert.Equal("default", Assert.IsType<StateDetail>(loaded.Saga.Detail).Label);

        loaded.Saga.Revision = 2;
        loaded.Saga.Detail = new StateDetail { Label = "schema-updated" };
        await repository.UpdateAsync(loaded, token);
        var publicRepository = (ILoadSagaRepository<EvolvingState>)AzureTableSagaRepository.Create<EvolvingState>(() => fixture.Table);
        EvolvingState reloaded = Assert.IsType<EvolvingState>(await publicRepository.LoadAsync(state.CorrelationId, token));
        Assert.Equal(state.CorrelationId, reloaded.CorrelationId);
        Assert.Equal(2, reloaded.Revision);
        Assert.Equal(explicitValues ? 0 : 7, reloaded.Count);
        Assert.Equal(!explicitValues, reloaded.Enabled);
        Assert.Equal(explicitValues ? string.Empty : "default", reloaded.Text);
        Assert.Equal(explicitValues ? Array.Empty<byte>() : new byte[] { 9 }, reloaded.Bytes);
        Assert.Equal("schema-updated", Assert.IsType<StateDetail>(reloaded.Detail).Label);
        TableEntity stored = (await fixture.Table.GetEntityAsync<TableEntity>(row.PartitionKey, row.RowKey, cancellationToken: token)).Value;
        Assert.Equal(explicitValues ? 0 : 7, Assert.IsType<int>(stored["Saga_Count"]));
        Assert.Equal(!explicitValues, Assert.IsType<bool>(stored["Saga_Enabled"]));
        Assert.Equal(explicitValues ? string.Empty : "default", Assert.IsType<string>(stored["Saga_Text"]));
        Assert.Equal(explicitValues ? Array.Empty<byte>() : new byte[] { 9 }, Assert.IsType<byte[]>(stored["Saga_Bytes"]));
    }

    internal static void AssertSameRow(TableEntity expected, TableEntity actual)
    {
        Assert.Equal(expected.ETag, actual.ETag);
        Assert.Equal(expected.Keys.Order(), actual.Keys.Order());
        foreach (string key in expected.Keys)
        {
            if (expected[key] is byte[] bytes)
                Assert.Equal(bytes, Assert.IsType<byte[]>(actual[key]));
            else
                Assert.Equal(expected[key], actual[key]);
        }
    }

    private static bool ContainsFailure(ExceptionInfo info, string type) =>
        info.ExceptionType == type || info.InnerException is not null && ContainsFailure(info.InnerException, type);

    public sealed record ApplyChange(Guid CorrelationId, Guid CommandId) : ICorrelatedBy<Guid>;
    public sealed record ChangeApplied(Guid CorrelationId, Guid CommandId, int Count);
    public sealed class StateDetail
    {
        public string Label { get; set; } = "default";
    }

    public enum ProcessingStage { Ready, Completed }

    public sealed class IntegrityState : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = "Active";
        public int Count { get; set; } = 17;
        public Guid Owner { get; set; }
        public bool? Enabled { get; set; } = true;
        public decimal Amount { get; set; } = 1234567890.123456789m;
        public ProcessingStage Stage { get; set; } = ProcessingStage.Ready;
        public StateDetail Detail { get; set; } = new() { Label = "original" };
    }

    public sealed class EvolvingState : ISaga
    {
        public Guid CorrelationId { get; set; }
        public int Revision { get; set; } = 1;
        public int? Count { get; set; } = 7;
        public bool? Enabled { get; set; } = true;
        public string? Text { get; set; } = "default";
        public byte[]? Bytes { get; set; } = [9];
        public StateDetail? Detail { get; set; } = new();
    }

    public sealed class HandlerProbe
    {
        private int _count;
        public int Count => Volatile.Read(ref _count);
        public void Enter() => Interlocked.Increment(ref _count);
    }

    public sealed class IntegrityMachine : ViciOneServiceBusStateMachine<IntegrityState>
    {
        public IntegrityMachine(HandlerProbe probe)
        {
            InstanceState(state => state.CurrentState);
            Event(() => Apply, configuration => configuration.CorrelateById(context => context.Message.CorrelationId));
            During(Active, When(Apply)
                .Then(context => { probe.Enter(); context.Saga.Count++; })
                .Publish(context => new ChangeApplied(context.Saga.CorrelationId, context.Message.CommandId, context.Saga.Count)));
        }

        public IState Active { get; private set; } = null!;
        public IEvent<ApplyChange> Apply { get; private set; } = null!;
    }

    private sealed class IntegrityDefinition : SagaDefinition<IntegrityState>
    {
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpointConfigurator,
            ISagaConfigurator<IntegrityState> sagaConfigurator, IRegistrationContext context) =>
            sagaConfigurator.UseVolatileOutbox(context);
    }
}
