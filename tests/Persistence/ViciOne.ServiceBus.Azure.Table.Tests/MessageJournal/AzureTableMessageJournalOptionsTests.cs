using System.Text.Json;
using global::Azure.Data.Tables;
using ViciOne.ServiceBus.Azure.Table;
using ViciOne.ServiceBus.Azure.Table.MessageJournal;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.MessageJournal;
using Xunit;

namespace ViciOne.ServiceBus.Azure.Table.Tests.MessageJournal;

public sealed class AzureTableMessageJournalOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-BOUNDS", "atomic-batch-capacity-boundary")]
    public void Capacity_AllowsTheAtomicMaximumAndRejectsOneMoreEntry()
    {
        var maximum = new MessageJournalStoreLimits(
            AzureTableMessageJournalStoreOptions.MaximumPropertyBytes,
            AzureTableMessageJournalStoreOptions.MaximumJournalEntriesPerPartition,
            TimeSpan.FromDays(1));
        var excessive = new MessageJournalStoreLimits(
            AzureTableMessageJournalStoreOptions.MaximumPropertyBytes,
            AzureTableMessageJournalStoreOptions.MaximumJournalEntriesPerPartition + 1,
            TimeSpan.FromDays(1));

        var options = new AzureTableMessageJournalStoreOptions("journal", maximum);
        var failure = Assert.Throws<ArgumentOutOfRangeException>(
            () => new AzureTableMessageJournalStoreOptions("journal", excessive));

        Assert.Same(maximum, options.Limits);
        Assert.Equal("limits", failure.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-BOUNDARY", "options-store-and-entry-inputs-fail-fast")]
    public async Task OptionsStoreAndEntryInputs_FailBeforeNetworkUseAsync()
    {
        MessageJournalStoreLimits limits = Limits();
        var excessiveProperty = new MessageJournalStoreLimits(
            AzureTableMessageJournalStoreOptions.MaximumPropertyBytes + 1,
            10,
            TimeSpan.FromDays(1));
        TableClient table = CreateTableClient();
        var options = new AzureTableMessageJournalStoreOptions("journal", limits);

        Assert.Equal("partitionKey", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableMessageJournalStoreOptions(null!, limits)).ParamName);
        Assert.Equal("partitionKey", Assert.Throws<ArgumentException>(() =>
            new AzureTableMessageJournalStoreOptions(" ", limits)).ParamName);
        Assert.Equal("limits", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableMessageJournalStoreOptions("journal", null!)).ParamName);
        Assert.Equal("limits", Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AzureTableMessageJournalStoreOptions("journal", excessiveProperty)).ParamName);
        Assert.Equal("table", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableMessageJournalStore(null!, options)).ParamName);
        Assert.Equal("options", Assert.Throws<ArgumentNullException>(() =>
            new AzureTableMessageJournalStore(table, null!)).ParamName);

        var store = new AzureTableMessageJournalStore(table, options);
        ArgumentNullException nullEntry = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await store.AppendAsync(null!, TestContext.Current.CancellationToken));
        Assert.Equal("entry", nullEntry.ParamName);

        MessageJournalEntry excessiveEntry = MessageJournalEntryTestFactory.Create(
            Guid.Parse("018cc251-f400-7000-8000-000000000012"),
            new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
            body: new byte[limits.MaximumEntryBytes + 1]);
        ArgumentOutOfRangeException excessiveFailure = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await store.AppendAsync(excessiveEntry, TestContext.Current.CancellationToken));
        Assert.Equal("entry", excessiveFailure.ParamName);

        MessageJournalEntry preAzureTimestamp = MessageJournalEntryTestFactory.Create(
            Guid.Parse("018cc251-f400-7000-8000-000000000014"),
            new DateTimeOffset(1600, 12, 31, 23, 59, 59, TimeSpan.Zero));
        ArgumentOutOfRangeException timestampFailure = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await store.AppendAsync(preAzureTimestamp, TestContext.Current.CancellationToken));
        Assert.Equal("entry", timestampFailure.ParamName);

        var maximumPropertyStore = new AzureTableMessageJournalStore(
            table,
            new AzureTableMessageJournalStoreOptions(
                "journal",
                new MessageJournalStoreLimits(
                    AzureTableMessageJournalStoreOptions.MaximumPropertyBytes,
                    10,
                    TimeSpan.FromDays(1))));
        MessageJournalEntry oversizedSerializedProperty = MessageJournalEntryTestFactory.Create(
            Guid.Parse("018cc251-f400-7000-8000-000000000015"),
            new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["value"] = new string('x', 32_768),
            });
        ArgumentOutOfRangeException propertyFailure = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await maximumPropertyStore.AppendAsync(oversizedSerializedProperty, TestContext.Current.CancellationToken));
        Assert.Equal(nameof(MessageJournalRecord.MetadataJson), propertyFailure.ParamName);

        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        OperationCanceledException cancellation = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await store.AppendAsync(
                MessageJournalEntryTestFactory.Create(
                    Guid.Parse("018cc251-f400-7000-8000-000000000016"),
                    new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero)),
                canceled.Token));
        Assert.Equal(canceled.Token, cancellation.CancellationToken);

        Assert.Equal("entry", Assert.Throws<ArgumentNullException>(() =>
            MessageJournalRecord.FromEntry(null!, "journal")).ParamName);
        Assert.Equal("partitionKey", Assert.Throws<ArgumentException>(() =>
            MessageJournalRecord.FromEntry(
                MessageJournalEntryTestFactory.Create(
                    Guid.Parse("018cc251-f400-7000-8000-000000000013"),
                    new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero)),
                "invalid/key")).ParamName);
    }

    [Theory]
    [InlineData("invalid/key")]
    [InlineData("invalid\\key")]
    [InlineData("invalid#key")]
    [InlineData("invalid?key")]
    [InlineData("invalid\u001fkey")]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-KEY", "unsafe-partition-keys-rejected")]
    public void PartitionKey_RejectsEveryAzureUnsafeCharacter(string partitionKey)
    {
        var failure = Assert.Throws<ArgumentException>(() => new AzureTableMessageJournalStoreOptions(
            partitionKey,
            Limits()));

        Assert.Equal(nameof(partitionKey), failure.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-KEY", "official-1024-character-boundary")]
    public void PartitionKey_UsesTheOfficial1024CharacterBoundary()
    {
        string maximumAscii = new('a', 1024);
        string maximumUnicode = new('\u00e9', 1024);
        string excessive = new('a', 1025);

        var acceptedAscii = new AzureTableMessageJournalStoreOptions(maximumAscii, Limits());
        var acceptedUnicode = new AzureTableMessageJournalStoreOptions(maximumUnicode, Limits());
        var failure = Assert.Throws<ArgumentException>(
            () => new AzureTableMessageJournalStoreOptions(excessive, Limits()));

        Assert.Equal(maximumAscii, acceptedAscii.PartitionKey);
        Assert.Equal(maximumUnicode, acceptedUnicode.PartitionKey);
        Assert.Equal("partitionKey", failure.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-CONFIGURATION", "constructor-has-no-network-side-effect")]
    public void StoreConstruction_PreservesLimitsWithoutPerformingNetworkIo()
    {
        var credential = new TableSharedKeyCredential(
            "localaccount",
            Convert.ToBase64String(new byte[32]));
        var table = new TableClient(
            new Uri("http://127.0.0.1:1/localaccount"),
            "journal",
            credential);
        var limits = Limits();

        var store = new AzureTableMessageJournalStore(
            table,
            new AzureTableMessageJournalStoreOptions("journal", limits));

        Assert.Same(limits, store.Limits);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-CONFIGURATION", "record-serialization-is-runtime-ready")]
    public void RecordConversion_SerializesTheSanitizedCollectionsWithoutAmbientJsonConfiguration()
    {
        MessageJournalEntry entry = MessageJournalEntryTestFactory.Create(
            Guid.Parse("018cc251-f400-7000-8000-000000000010"),
            new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
            messageTypes: ["urn:message:first", "urn:message:second"],
            metadata: new Dictionary<string, string> { ["correlationId"] = "abc" },
            headers: new Dictionary<string, string> { ["tenant"] = "north" });

        MessageJournalRecord record = MessageJournalRecord.FromEntry(entry, "journal");

        Assert.Equal(2, JsonDocument.Parse(record.MessageTypesJson).RootElement.GetArrayLength());
        Assert.Equal("abc", JsonDocument.Parse(record.MetadataJson).RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("north", JsonDocument.Parse(record.HeadersJson).RootElement.GetProperty("tenant").GetString());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AZURE-TABLE-MESSAGE-JOURNAL-CONFIGURATION", "pre-azure-timestamps-rejected-before-network-io")]
    public void RecordConversion_RejectsTimestampsThatAzureTableCannotRepresent()
    {
        MessageJournalEntry entry = MessageJournalEntryTestFactory.Create(
            Guid.Parse("018cc251-f400-7000-8000-000000000011"),
            new DateTimeOffset(1600, 12, 31, 23, 59, 59, TimeSpan.Zero));

        var failure = Assert.Throws<ArgumentOutOfRangeException>(
            () => MessageJournalRecord.FromEntry(entry, "journal"));

        Assert.Equal("entry", failure.ParamName);
    }

    private static MessageJournalStoreLimits Limits() => new(
        maximumEntryBytes: 4096,
        maximumEntries: 10,
        retentionPeriod: TimeSpan.FromDays(1));

    private static TableClient CreateTableClient()
    {
        var credential = new TableSharedKeyCredential(
            "localaccount",
            Convert.ToBase64String(new byte[32]));
        return new TableClient(
            new Uri("http://127.0.0.1:1/localaccount"),
            "journal",
            credential);
    }
}
