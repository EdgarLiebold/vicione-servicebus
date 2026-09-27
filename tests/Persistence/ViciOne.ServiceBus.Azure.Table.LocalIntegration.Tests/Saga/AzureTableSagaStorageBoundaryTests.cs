using global::Azure.Core;
using global::Azure.Core.Pipeline;
using global::Azure.Data.Tables;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Azure.Table.Saga;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Saga;

public sealed class AzureTableSagaStorageBoundaryTests
{
    [Theory]
    [InlineData("BmpText")]
    [InlineData("SurrogateText")]
    [InlineData("Bytes")]
    [InlineData("UtcTime")]
    [InlineData("OffsetTime")]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-SAGA-BOUNDS", "exact-limit-admitted-invalid-insert-update-have-no-storage-effect")]
    public async Task StorageBoundary_RejectsWritesWithoutEffectsAndAcceptsExactLimitAsync(string boundary)
    {
        CancellationToken token = TestContext.Current.CancellationToken;
        var writes = new WriteCounter();
        var options = new TableClientOptions();
        options.AddPolicy(writes, HttpPipelinePosition.PerCall);
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("SagaBounds", token, options);
        var storage = new AzureTableSagaStorageContext<BoundedState>(fixture.Table, new FixedPartitionSagaKeyFormatter(nameof(BoundedState)));
        ConsumeContext<BoundaryMessage> consumeContext = InMemoryOutboxTestContextFactory.Create(new BoundaryMessage(), token);
        var repository = new AzureTableSagaRepositoryContext<BoundedState, BoundaryMessage>(storage, consumeContext,
            new SagaConsumeContextFactory<IAzureTableSagaStorageContext<BoundedState>, BoundedState>());
        var state = new BoundedState { CorrelationId = Guid.NewGuid() };
        var neighbor = new BoundedState { CorrelationId = Guid.NewGuid(), Text = "neighbor", Revision = 41 };
        await repository.SaveAsync(await repository.AddAsync(neighbor, token), token);
        var (neighborPartition, neighborKey) = storage.Format(neighbor.CorrelationId);
        TableEntity neighborBefore = (await fixture.Table.GetEntityAsync<TableEntity>(neighborPartition, neighborKey, cancellationToken: token)).Value;
        var (partition, key) = storage.Format(state.CorrelationId);
        SagaConsumeContext<BoundedState, BoundaryMessage> added = await repository.AddAsync(state, token);
        SetBoundary(state, boundary, valid: false);
        int initialWrites = writes.Count;

        SagaException insertFailure = await Assert.ThrowsAsync<SagaException>(() => repository.SaveAsync(added, token));
        Assert.Equal(state.CorrelationId, insertFailure.CorrelationId);
        Assert.Contains(PropertyName(boundary), Assert.IsType<InvalidOperationException>(insertFailure.InnerException).Message, StringComparison.Ordinal);
        Assert.Equal(initialWrites, writes.Count);
        Assert.False((await fixture.Table.GetEntityIfExistsAsync<TableEntity>(partition, key, cancellationToken: token)).HasValue);

        SetBoundary(state, boundary, valid: true);
        await repository.SaveAsync(added, token);
        Assert.Equal(initialWrites + 1, writes.Count);
        SagaConsumeContext<BoundedState, BoundaryMessage> loaded = Assert.IsAssignableFrom<SagaConsumeContext<BoundedState, BoundaryMessage>>(
            await repository.LoadAsync(state.CorrelationId, token));
        AssertBoundary(state, loaded.Saga);
        TableEntity before = (await fixture.Table.GetEntityAsync<TableEntity>(partition, key, cancellationToken: token)).Value;
        SetBoundary(loaded.Saga, boundary, valid: false);
        loaded.Saga.Revision = 2;
        int beforeUpdateWrites = writes.Count;

        SagaException updateFailure = await Assert.ThrowsAsync<SagaException>(() => repository.UpdateAsync(loaded, token));
        Assert.Equal(state.CorrelationId, updateFailure.CorrelationId);
        Assert.Contains(PropertyName(boundary), Assert.IsType<InvalidOperationException>(updateFailure.InnerException).Message, StringComparison.Ordinal);
        Assert.Equal(beforeUpdateWrites, writes.Count);
        AzureTableSagaIntegrityTests.AssertSameRow(before,
            (await fixture.Table.GetEntityAsync<TableEntity>(partition, key, cancellationToken: token)).Value);
        AzureTableSagaIntegrityTests.AssertSameRow(neighborBefore,
            (await fixture.Table.GetEntityAsync<TableEntity>(neighborPartition, neighborKey, cancellationToken: token)).Value);

        SetBoundary(loaded.Saga, boundary, valid: true);
        await repository.UpdateAsync(loaded, token);
        Assert.Equal(beforeUpdateWrites + 1, writes.Count);
        var publicRepository = (ILoadSagaRepository<BoundedState>)AzureTableSagaRepository.Create<BoundedState>(() => fixture.Table);
        BoundedState persisted = Assert.IsType<BoundedState>(await publicRepository.LoadAsync(state.CorrelationId, token));
        AssertBoundary(loaded.Saga, persisted);
        Assert.Equal(2, persisted.Revision);
        Assert.NotEqual(before.ETag, (await fixture.Table.GetEntityAsync<TableEntity>(partition, key, cancellationToken: token)).Value.ETag);
        AzureTableSagaIntegrityTests.AssertSameRow(neighborBefore,
            (await fixture.Table.GetEntityAsync<TableEntity>(neighborPartition, neighborKey, cancellationToken: token)).Value);
    }

    private static string PropertyName(string boundary) => boundary is "BmpText" or "SurrogateText" ? "Text" : boundary;

    private static void SetBoundary(BoundedState state, string boundary, bool valid)
    {
        switch (boundary)
        {
            case "BmpText":
                state.Text = new string('\u20ac', 32768) + (valid ? string.Empty : "x");
                break;
            case "SurrogateText":
                state.Text = string.Concat(Enumerable.Repeat("\U0001F680", 16384)) + (valid ? string.Empty : "x");
                break;
            case "Bytes":
                state.Bytes = Enumerable.Repeat((byte)0xa5, valid ? 65536 : 65537).ToArray();
                break;
            case "UtcTime":
                state.UtcTime = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(valid ? 0 : -1);
                break;
            case "OffsetTime":
                state.OffsetTime = new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero)
                    .AddMilliseconds(valid ? 0 : -1).ToOffset(TimeSpan.FromHours(-1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(boundary));
        }
    }

    private static void AssertBoundary(BoundedState expected, BoundedState actual)
    {
        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(expected.Text, actual.Text);
        Assert.Equal(expected.Bytes, actual.Bytes);
        Assert.Equal(expected.UtcTime, actual.UtcTime);
        Assert.Equal(expected.OffsetTime.UtcDateTime, actual.OffsetTime.UtcDateTime);
    }

    private sealed class WriteCounter : HttpPipelineSynchronousPolicy
    {
        private int _count;
        public int Count => Volatile.Read(ref _count);
        public override void OnSendingRequest(HttpMessage message)
        {
            if (message.Request.Method != RequestMethod.Get && message.Request.Method != RequestMethod.Head)
                Interlocked.Increment(ref _count);
        }
    }

    public sealed record BoundaryMessage;

    public sealed class BoundedState : ISaga
    {
        public Guid CorrelationId { get; set; }
        public int Revision { get; set; } = 1;
        public string Text { get; set; } = "initial";
        public byte[] Bytes { get; set; } = [1, 2, 3];
        public DateTime UtcTime { get; set; } = new(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc);
        public DateTimeOffset OffsetTime { get; set; } = new(2026, 9, 6, 13, 0, 0, TimeSpan.FromHours(1));
    }
}
