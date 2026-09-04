using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ViciOne.ServiceBus.EntityFrameworkCore.MessageJournal;
using ViciOne.ServiceBus.MessageJournal;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.MessageJournal;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCore.Tests.MessageJournal;

public sealed class EntityFrameworkMessageJournalModelTests
{
    [Theory]
    [InlineData("sql-server")]
    [InlineData("postgresql")]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-MAPPING", "provider-neutral-relational-model")]
    public void Model_MapsTheBoundedRecordForEverySupportedRelationalProvider(string provider)
    {
        DbContextOptions options = CreateOptions(provider);
        using var context = new MessageJournalDbContext(options, "MessageJournal", "journal");

        IEntityType entity = context.Model.FindEntityType(typeof(MessageJournalRecord))
            ?? throw new InvalidOperationException("MessageJournalRecord is not mapped.");

        Assert.Equal("MessageJournal", entity.GetTableName());
        Assert.Equal("journal", entity.GetSchema());
        Assert.Equal(nameof(MessageJournalRecord.EntryId), Assert.Single(entity.FindPrimaryKey()!.Properties).Name);
        Assert.Contains(entity.GetIndexes(), index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(MessageJournalRecord.ObservedAt), nameof(MessageJournalRecord.EntryId)]));
        Assert.Equal(256, entity.FindProperty(nameof(MessageJournalRecord.ContentType))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(MessageJournalRecord.Body))!.IsNullable);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-MAPPING", "dynamic-table-and-schema-model-cache")]
    public void ModelCacheKey_PreservesEachExplicitTableAndSchemaSelection()
    {
        DbContextOptions options = CreateOptions("sql-server");
        using var first = new MessageJournalDbContext(options, "JournalOne", "north");
        using var second = new MessageJournalDbContext(options, "JournalTwo", "south");

        IEntityType firstEntity = first.Model.FindEntityType(typeof(MessageJournalRecord))!;
        IEntityType secondEntity = second.Model.FindEntityType(typeof(MessageJournalRecord))!;

        Assert.Equal(("JournalOne", "north"), (firstEntity.GetTableName(), firstEntity.GetSchema()));
        Assert.Equal(("JournalTwo", "south"), (secondEntity.GetTableName(), secondEntity.GetSchema()));
        Assert.NotSame(first.Model, second.Model);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-BOUNDS", "store-declares-explicit-finite-limits")]
    public void Store_PreservesTheExactFiniteLimitsWithoutConnectingToADatabase()
    {
        var limits = new MessageJournalStoreLimits(4096, 25, TimeSpan.FromDays(2));

        var store = new EntityFrameworkMessageJournalStore(
            CreateOptions("postgresql"),
            "MessageJournal",
            limits);

        Assert.Same(limits, store.Limits);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EF-MESSAGE-JOURNAL-MAPPING", "record-serialization-is-runtime-ready")]
    public void RecordConversion_SerializesTheSanitizedCollectionsWithoutAmbientJsonConfiguration()
    {
        MessageJournalEntry entry = MessageJournalEntryTestFactory.Create(
            Guid.Parse("018cc251-f400-7000-8000-000000000020"),
            new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero),
            messageTypes: ["urn:message:first", "urn:message:second"],
            metadata: new Dictionary<string, string> { ["correlationId"] = "abc" },
            headers: new Dictionary<string, string> { ["tenant"] = "north" });

        MessageJournalRecord record = MessageJournalRecord.FromEntry(entry);

        Assert.Equal(2, JsonDocument.Parse(record.MessageTypesJson).RootElement.GetArrayLength());
        Assert.Equal("abc", JsonDocument.Parse(record.MetadataJson).RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("north", JsonDocument.Parse(record.HeadersJson).RootElement.GetProperty("tenant").GetString());
    }

    private static DbContextOptions CreateOptions(string provider)
    {
        var builder = new DbContextOptionsBuilder();
        return provider switch
        {
            "sql-server" => builder.UseSqlServer(
                "Server=localhost;Database=not-opened;User Id=unused;Password=unused;Encrypt=False").Options,
            "postgresql" => builder.UseNpgsql(
                "Host=localhost;Database=not_opened;Username=unused;Password=unused").Options,
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
        };
    }
}
