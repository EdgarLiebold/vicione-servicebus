using global::Azure;
using global::Azure.Core;
using global::Azure.Data.Tables;
using ViciOne.ServiceBus.AzureTable.MessageJournal;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.MessageJournal;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.Tests.MessageJournal;

public sealed class AzureTableMessageJournalStoreTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-BOUNDS", "atomic-repair-is-one-ordered-transaction")]
    public async Task Append_SubmitsLeasePruningAndEntryAsOneOrderedTransactionAsync()
    {
        const string partitionKey = "journal";
        var now = new DateTimeOffset(2030, 1, 12, 12, 0, 0, TimeSpan.Zero);
        var lease = new MessageJournalCapacityLease
        {
            PartitionKey = partitionKey,
            ETag = new ETag("W/\"lease-7\""),
            Generation = 7,
        };
        MessageJournalRecord expired = Record(
            "018cc251-f400-7000-8000-000000000401",
            now.AddDays(-11),
            partitionKey,
            "W/\"expired\"");
        MessageJournalRecord oldestRetained = Record(
            "018cc251-f400-7000-8000-000000000402",
            now.AddHours(-2),
            partitionKey,
            "W/\"oldest\"");
        MessageJournalRecord newestRetained = Record(
            "018cc251-f400-7000-8000-000000000403",
            now.AddHours(-1),
            partitionKey,
            "W/\"newest\"");
        var table = new RecordingTableClient(lease, [expired, oldestRetained, newestRetained]);
        var store = new AzureTableMessageJournalStore(
            table,
            new AzureTableMessageJournalStoreOptions(
                partitionKey,
                new MessageJournalStoreLimits(4096, 2, TimeSpan.FromDays(10))));
        MessageJournalEntry appended = Entry("018cc251-f400-7000-8000-000000000404", now);

        await store.AppendAsync(appended, TestContext.Current.CancellationToken);

        Assert.Equal(1, table.SubmitCallCount);
        Assert.Equal(0, table.IndividualWriteCallCount);
        Assert.Collection(
            Assert.IsAssignableFrom<IReadOnlyList<TableTransactionAction>>(table.SubmittedActions),
            action =>
            {
                Assert.Equal(TableTransactionActionType.UpdateReplace, action.ActionType);
                Assert.Same(lease, action.Entity);
                Assert.Equal(new ETag("W/\"lease-7\""), action.ETag);
                Assert.Equal(8, lease.Generation);
            },
            action => AssertDelete(action, expired),
            action => AssertDelete(action, oldestRetained),
            action =>
            {
                Assert.Equal(TableTransactionActionType.Add, action.ActionType);
                MessageJournalRecord record = Assert.IsType<MessageJournalRecord>(action.Entity);
                Assert.Equal(appended.EntryId, record.EntryId);
                Assert.Equal(partitionKey, record.PartitionKey);
            });
    }

    private static void AssertDelete(TableTransactionAction action, MessageJournalRecord expected)
    {
        Assert.Equal(TableTransactionActionType.Delete, action.ActionType);
        Assert.Same(expected, action.Entity);
        Assert.Equal(expected.ETag, action.ETag);
    }

    private static MessageJournalRecord Record(
        string id,
        DateTimeOffset observedAt,
        string partitionKey,
        string eTag)
    {
        MessageJournalRecord record = MessageJournalRecord.FromEntry(Entry(id, observedAt), partitionKey);
        record.ETag = new ETag(eTag);
        return record;
    }

    private static MessageJournalEntry Entry(string id, DateTimeOffset observedAt) =>
        MessageJournalEntryTestFactory.Create(
            Guid.Parse(id),
            observedAt,
            MessageJournalOperation.Publish,
            MessageJournalOutcome.Succeeded,
            MessageJournalDataClassification.Internal,
            "application/json",
            ["urn:message:journal"],
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            "body"u8.ToArray());

    private sealed class RecordingTableClient(
        MessageJournalCapacityLease lease,
        IReadOnlyList<MessageJournalRecord> records) : TableClient
    {
        private readonly StubResponse _response = new();

        public int IndividualWriteCallCount { get; private set; }

        public int SubmitCallCount { get; private set; }

        public IReadOnlyList<TableTransactionAction>? SubmittedActions { get; private set; }

        public override Task<global::Azure.Response<T>> GetEntityAsync<T>(
            string partitionKey,
            string rowKey,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::Azure.Response<T>>(cancellationToken); return Task.FromResult(global::Azure.Response.FromValue((T)(object)lease, _response)); }
        public override AsyncPageable<T> QueryAsync<T>(
            string? filter = null,
            int? maxPerPage = null,
            IEnumerable<string>? select = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested(); return AsyncPageable<T>.FromPages(
                [Page<T>.FromValues(records.Cast<T>().ToArray(), continuationToken: null, _response)]);
        }
        public override Task<global::Azure.Response<IReadOnlyList<global::Azure.Response>>> SubmitTransactionAsync(
            IEnumerable<TableTransactionAction> transactionActions,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::Azure.Response<global::System.Collections.Generic.IReadOnlyList<global::Azure.Response>>>(cancellationToken); SubmitCallCount++;
            SubmittedActions = transactionActions.ToArray();
            return Task.FromResult(global::Azure.Response.FromValue<IReadOnlyList<global::Azure.Response>>([], _response));
        }

        public override Task<global::Azure.Response> AddEntityAsync<T>(T entity, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::Azure.Response>(cancellationToken); IndividualWriteCallCount++;
            throw new InvalidOperationException("Journal append must not split the transaction into individual writes.");
        }

        public override Task<global::Azure.Response> UpdateEntityAsync<T>(
            T entity,
            ETag ifMatch,
            TableUpdateMode mode = TableUpdateMode.Merge,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::Azure.Response>(cancellationToken); IndividualWriteCallCount++;
            throw new InvalidOperationException("Journal append must not split the transaction into individual writes.");
        }

        public override Task<global::Azure.Response> DeleteEntityAsync(
            string partitionKey,
            string rowKey,
            ETag ifMatch = default,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::Azure.Response>(cancellationToken); IndividualWriteCallCount++;
            throw new InvalidOperationException("Journal append must not split the transaction into individual writes.");
        }
    }

    private sealed class StubResponse : global::Azure.Response
    {
        public override int Status => 200;

        public override string ReasonPhrase => "OK";

        public override Stream? ContentStream { get; set; }

        public override string ClientRequestId { get; set; } = "azure-table-transaction-test";

        public override void Dispose()
        {
        }

        protected override bool ContainsHeader(string name) => false;

        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];

        protected override bool TryGetHeader(string name, out string value)
        {
            value = null!;
            return false;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            values = null!;
            return false;
        }
    }
}
