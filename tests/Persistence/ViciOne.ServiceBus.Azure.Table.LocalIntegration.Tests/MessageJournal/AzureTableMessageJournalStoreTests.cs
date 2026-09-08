using System.Text.Json;
using global::Azure;
using global::Azure.Core;
using global::Azure.Core.Pipeline;
using global::Azure.Data.Tables;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Azure.Table;
using ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Azure.Table.MessageJournal;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.MessageJournal;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.LocalIntegration.Tests.MessageJournal;

public sealed class AzureTableMessageJournalStoreTests
{
    [Theory]
    [InlineData(MessageJournalOperation.Send)]
    [InlineData(MessageJournalOperation.Consume)]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-PERSISTENCE", "send-and-consume-terminal-operations-round-trip")]
    public async Task Append_PreservesSendAndConsumeTerminalOperationsAsync(MessageJournalOperation operation)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("JournalOperation", cancellationToken);
        var store = CreateStore(fixture.Table, Limits(maximumEntries: 2));
        MessageJournalEntry expected = MessageJournalEntryTestFactory.Create(
            Guid.CreateVersion7(),
            new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
            operation,
            MessageJournalOutcome.Succeeded,
            MessageJournalDataClassification.Internal,
            "application/json",
            ["urn:message:journal-operation"],
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            "terminal"u8.ToArray());

        await store.AppendAsync(expected, cancellationToken);

        MessageJournalRecord actual = Assert.Single(await ReadEntriesAsync(fixture.Table, cancellationToken));
        Assert.Equal(operation.ToString(), actual.Operation);
        Assert.Equal(MessageJournalOutcome.Succeeded.ToString(), actual.Outcome);
        Assert.Equal(expected.EntryId, actual.EntryId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-CONFIGURATION", "table-client-composition-persists-terminal-envelope")]
    public async Task TableClientComposition_PersistsATerminalPublishEnvelopeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("Journal", cancellationToken);
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configurator =>
            configurator.UseAzureTableMessageJournal(
                fixture.Table,
                new AzureTableMessageJournalStoreOptions("journal", Limits(maximumEntries: 10)),
                PassThroughPolicy(),
                JournalOptions()));

        await bus.StartAsync(cancellationToken);
        try
        {
            await bus.PublishAsync(new JournalProbe("table-client"), cancellationToken);

            MessageJournalRecord actual = Assert.Single(await ReadEntriesAsync(fixture.Table, cancellationToken));
            Assert.Equal(MessageJournalOperation.Publish.ToString(), actual.Operation);
            Assert.Contains(nameof(JournalProbe), actual.MessageTypesJson, StringComparison.Ordinal);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None)
                .WaitAsync(OperationTimeout(), CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-CONFIGURATION", "service-client-composition-persists-terminal-envelope")]
    public async Task TableServiceClientComposition_PersistsATerminalPublishEnvelopeAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("Journal", cancellationToken);
        IBusControl bus = Bus.Factory.CreateUsingInMemory(configurator =>
            configurator.UseAzureTableMessageJournal(
                fixture.Service,
                fixture.Table.Name,
                new AzureTableMessageJournalStoreOptions("journal", Limits(maximumEntries: 10)),
                PassThroughPolicy(),
                JournalOptions()));

        await bus.StartAsync(cancellationToken);
        try
        {
            await bus.PublishAsync(new JournalProbe("service-client"), cancellationToken);

            MessageJournalRecord actual = Assert.Single(await ReadEntriesAsync(fixture.Table, cancellationToken));
            Assert.Equal(MessageJournalOperation.Publish.ToString(), actual.Operation);
            Assert.Contains(nameof(JournalProbe), actual.MessageTypesJson, StringComparison.Ordinal);
        }
        finally
        {
            await bus.StopAsync(CancellationToken.None)
                .WaitAsync(OperationTimeout(), CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-PERSISTENCE", "sanitized-entry-round-trip-and-collision-free-identity")]
    public async Task Append_PreservesSanitizedFieldsAndSeparatesEntriesAtTheSameTimestampAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("Journal", cancellationToken);
        var store = CreateStore(fixture.Table, Limits(maximumEntries: 10));
        var observedAt = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);
        MessageJournalEntry first = Entry(
            Guid.Parse("018cc251-f400-7000-8000-000000000001"),
            observedAt,
            "first"u8.ToArray());
        MessageJournalEntry second = Entry(
            Guid.Parse("018cc251-f400-7000-8000-000000000002"),
            observedAt,
            "second"u8.ToArray());

        await store.AppendAsync(first, cancellationToken);
        await store.AppendAsync(second, cancellationToken);

        MessageJournalRecord[] records = await ReadEntriesAsync(fixture.Table, cancellationToken);
        Assert.Equal(2, records.Length);
        Assert.Equal(2, records.Select(record => record.RowKey).Distinct(StringComparer.Ordinal).Count());
        MessageJournalRecord actual = records.Single(record => record.EntryId == first.EntryId);
        Assert.Equal(first.ObservedAt, actual.ObservedAt);
        Assert.Equal(first.Operation.ToString(), actual.Operation);
        Assert.Equal(first.Outcome.ToString(), actual.Outcome);
        Assert.Equal(first.DataClassification.ToString(), actual.DataClassification);
        Assert.Equal(first.ContentType, actual.ContentType);
        Assert.Equal(first.Body.ToArray(), actual.Body);
        Assert.Equal(first.ContentSizeInBytes, actual.ContentSizeInBytes);
        Assert.Equal(first.MessageTypes, JsonSerializer.Deserialize<string[]>(actual.MessageTypesJson));
        Assert.Equal(first.Metadata, JsonSerializer.Deserialize<Dictionary<string, string>>(actual.MetadataJson));
        Assert.Equal(first.Headers, JsonSerializer.Deserialize<Dictionary<string, string>>(actual.HeadersJson));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-BOUNDS", "lowered-capacity-self-heals-atomically")]
    public async Task Append_AtomicallyAppliesRetentionAndALoweredCapacityAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("Journal", cancellationToken);
        var initialStore = CreateStore(fixture.Table, Limits(maximumEntries: 5, retentionPeriod: TimeSpan.FromDays(10)));
        var now = new DateTimeOffset(2030, 1, 10, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset[] initialTimes =
        [
            now.AddDays(-3),
            now.AddHours(-18),
            now.AddHours(-12),
            now.AddHours(-6),
            now.AddHours(-1),
        ];
        foreach (DateTimeOffset timestamp in initialTimes)
            await initialStore.AppendAsync(Entry(Guid.CreateVersion7(), timestamp), cancellationToken);

        var reducedStore = CreateStore(fixture.Table, Limits(maximumEntries: 2, retentionPeriod: TimeSpan.FromDays(1)));
        await reducedStore.AppendAsync(Entry(Guid.CreateVersion7(), now), cancellationToken);

        MessageJournalRecord[] records = await ReadEntriesAsync(fixture.Table, cancellationToken);
        Assert.Equal(2, records.Length);
        Assert.Equal([now.AddHours(-1), now], records.Select(record => record.ObservedAt).Order());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-CONCURRENCY", "etag-lease-capacity-never-exceeded")]
    public async Task ConcurrentAppends_NeverExceedTheDeclaredCapacityAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const int writerCount = 8;
        var barrier = new BatchSubmitBarrierPolicy(writerCount, OperationTimeout());
        var clientOptions = new TableClientOptions();
        clientOptions.AddPolicy(barrier, HttpPipelinePosition.PerCall);
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync(
            "Journal",
            cancellationToken,
            clientOptions);
        var store = CreateStore(fixture.Table, Limits(maximumEntries: 1));
        var observedAt = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

        Task<Exception?>[] attempts = Enumerable.Range(0, writerCount)
            .Select(index => TryAppendAsync(
                store,
                Entry(Guid.CreateVersion7(), observedAt.AddTicks(index)),
                cancellationToken))
            .ToArray();
        Exception? barrierFailure = await Record.ExceptionAsync(() =>
            barrier.WaitUntilAllBatchSubmitsArriveAsync(cancellationToken));
        barrier.Release();
        Exception?[]? outcomes = null;
        Exception? completionFailure = await Record.ExceptionAsync(async () =>
        {
            outcomes = await Task.WhenAll(attempts)
                .WaitAsync(OperationTimeout(), cancellationToken);
        });

        Assert.Null(barrierFailure);
        Assert.Null(completionFailure);
        Assert.NotNull(outcomes);
        Assert.Equal(writerCount, barrier.ArrivalCount);
        Assert.Single(await ReadEntriesAsync(fixture.Table, cancellationToken));
        Assert.Single(outcomes, outcome => outcome is null);
        Assert.Equal(writerCount - 1, outcomes.Count(outcome => outcome is not null));
        Assert.All(outcomes.Where(outcome => outcome is not null), outcome =>
        {
            RequestFailedException conflict = Assert.IsAssignableFrom<RequestFailedException>(outcome);
            Assert.Equal(412, conflict.Status);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-RETENTION", "minimum-timestamp-does-not-underflow")]
    public async Task MaximumRetention_AcceptsTheEarliestRepresentableObservationAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("Journal", cancellationToken);
        var store = CreateStore(fixture.Table, Limits(maximumEntries: 2, retentionPeriod: TimeSpan.MaxValue));
        MessageJournalEntry entry = Entry(
            Guid.CreateVersion7(),
            new DateTimeOffset(1601, 1, 1, 0, 0, 0, TimeSpan.Zero));

        await store.AppendAsync(entry, cancellationToken);

        Assert.Equal(entry.EntryId, Assert.Single(await ReadEntriesAsync(fixture.Table, cancellationToken)).EntryId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-OWNERSHIP", "foreign-rows-are-never-read-or-pruned")]
    public async Task Append_NeverTreatsForeignPartitionRowsAsJournalEntriesAsync()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AzureTableTestTable fixture = await AzureTableTestTable.CreateAsync("Journal", cancellationToken);
        var foreign = new TableEntity("journal", "application|state")
        {
            ["Value"] = "must-remain",
        };
        await fixture.Table.AddEntityAsync(foreign, cancellationToken);
        var store = CreateStore(fixture.Table, Limits(maximumEntries: 1));
        var observedAt = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

        await store.AppendAsync(Entry(Guid.CreateVersion7(), observedAt.AddSeconds(-1)), cancellationToken);
        await store.AppendAsync(Entry(Guid.CreateVersion7(), observedAt), cancellationToken);

        Assert.Single(await ReadEntriesAsync(fixture.Table, cancellationToken));
        global::Azure.Response<TableEntity> retained = await fixture.Table.GetEntityAsync<TableEntity>(
            foreign.PartitionKey,
            foreign.RowKey,
            cancellationToken: cancellationToken);
        Assert.Equal("must-remain", retained.Value.GetString("Value"));
    }

    private static async Task<Exception?> TryAppendAsync(
        IMessageJournalStore store,
        MessageJournalEntry entry,
        CancellationToken cancellationToken)
    {
        try
        {
            await store.AppendAsync(entry, cancellationToken);
            return null;
        }
        catch (RequestFailedException exception)
        {
            return exception;
        }
    }

    private static MessageJournalEntry Entry(Guid id, DateTimeOffset observedAt, byte[]? body = null) =>
        MessageJournalEntryTestFactory.Create(
            id,
            observedAt,
            MessageJournalOperation.Publish,
            MessageJournalOutcome.Succeeded,
            MessageJournalDataClassification.Confidential,
            "application/json",
            ["urn:message:journal"],
            new Dictionary<string, string>(StringComparer.Ordinal) { ["tenant"] = "north" },
            new Dictionary<string, string>(StringComparer.Ordinal) { ["traceparent"] = "safe" },
            body ?? "redacted"u8.ToArray());

    private static MessageJournalStoreLimits Limits(
        int maximumEntries,
        TimeSpan? retentionPeriod = null) => new(
            maximumEntryBytes: 4096,
            maximumEntries,
            retentionPeriod ?? TimeSpan.FromDays(10));

    private static AzureTableMessageJournalStore CreateStore(
        TableClient table,
        MessageJournalStoreLimits limits) =>
        new(table, new AzureTableMessageJournalStoreOptions("journal", limits));

    private static async Task<MessageJournalRecord[]> ReadEntriesAsync(
        TableClient table,
        CancellationToken cancellationToken)
    {
        var entries = new List<MessageJournalRecord>();
        await foreach (MessageJournalRecord record in table
                           .QueryAsync<MessageJournalRecord>(
                               record => record.PartitionKey == "journal",
                               cancellationToken: cancellationToken)
                           .WithCancellation(cancellationToken))
        {
            if (record.RowKey.StartsWith("entry|", StringComparison.Ordinal))
                entries.Add(record);
        }

        return entries.ToArray();
    }

    private static MessageJournalOptions JournalOptions() => MessageJournalOptions.ContinueMessageFlow(
        OperationTimeout(),
        TimeProvider.System);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedLocalOptions(LocalTestResource.AzureTable)
        .OperationTimeout!.Value;

    private static IMessageJournalPolicy PassThroughPolicy() => new PassThroughJournalPolicy();

    private sealed class PassThroughJournalPolicy : IMessageJournalPolicy
    {
        public ValueTask<MessageJournalProjection?> ProjectAsync(
            MessageJournalCapture capture,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled<global::ViciOne.ServiceBus.MessageJournal.MessageJournalProjection?>(cancellationToken); return ValueTask.FromResult<MessageJournalProjection?>(new MessageJournalProjection(
            MessageJournalDataClassification.Internal,
            capture.ContentType,
            capture.MessageTypes,
            capture.Metadata,
            capture.Headers,
            capture.Body));
        }
    }

    private sealed record JournalProbe(string Source);

    private sealed class BatchSubmitBarrierPolicy(int expectedArrivals, TimeSpan operationTimeout) :
        HttpPipelinePolicy
    {
        private readonly TaskCompletionSource _allArrived =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivalCount;

        public int ArrivalCount => Volatile.Read(ref _arrivalCount);

        public override void Process(
            HttpMessage message,
            ReadOnlyMemory<HttpPipelinePolicy> pipeline) =>
            throw new InvalidOperationException("The batch barrier supports asynchronous requests only.");

        public override async ValueTask ProcessAsync(
            HttpMessage message,
            ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            if (message.Request.Method == RequestMethod.Post
                && message.Request.Uri.ToUri().AbsolutePath.EndsWith("/$batch", StringComparison.Ordinal))
            {
                int arrivals = Interlocked.Increment(ref _arrivalCount);
                if (arrivals > expectedArrivals)
                    throw new InvalidOperationException("More batch submits arrived than the test declared.");
                if (arrivals == expectedArrivals)
                    _allArrived.TrySetResult();

                await _release.Task
                    .WaitAsync(operationTimeout, message.CancellationToken)
                    .ConfigureAwait(false);
            }

            await ProcessNextAsync(message, pipeline).ConfigureAwait(false);
        }

        public async Task WaitUntilAllBatchSubmitsArriveAsync(CancellationToken cancellationToken) =>
            await _allArrived.Task
                .WaitAsync(operationTimeout, cancellationToken)
                .ConfigureAwait(false);

        public void Release()
        {
            _release.TrySetResult();
        }
    }

}
