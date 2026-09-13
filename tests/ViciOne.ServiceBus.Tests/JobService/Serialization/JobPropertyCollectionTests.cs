using ViciOne.ServiceBus.JobService;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Serialization;

public sealed class JobPropertyCollectionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "case-insensitive-overwrite-preserve-and-remove")]
    public void Set_UsesCaseInsensitiveKeysAndExplicitMutationSemantics()
    {
        var properties = new JobPropertyCollection();

        properties.Set("Tenant", "north");
        properties.Set("TENANT", "south", overwrite: false);

        Assert.Equal(1, properties.Count);
        Assert.True(properties.TryGet("tenant", out object? preserved));
        Assert.Equal("north", preserved);

        properties.Set("tenant", "south");
        Assert.Equal("south", properties.Get<string>("TENANT"));

        properties.Set("TENANT", null);
        Assert.False(properties.TryGet("tenant", out _));
        Assert.Empty(properties);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "typed-values-and-defaults")]
    public void Get_ReturnsTypedValuesAndDefaults()
    {
        var properties = new JobPropertyCollection();
        properties.Set("attempts", 3);
        properties.Set("region", "eu-central");

        Assert.Equal(3, properties.Get<int>("ATTEMPTS"));
        Assert.Equal("eu-central", properties.Get<string>("REGION"));
        Assert.Equal(7, properties.Get<int>("missing", 7));
        Assert.Equal("fallback", properties.Get<string>("absent", "fallback"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "bulk-update-is-validated-before-mutation")]
    public void SetMany_RejectsInvalidKeysBeforeMutatingTheCollection()
    {
        var properties = new JobPropertyCollection();
        properties.Set("existing", 1);
        KeyValuePair<string, object?>[] updates =
        [
            new("valid", 2),
            new(" ", 3)
        ];

        Assert.Throws<ArgumentException>(() => properties.SetMany(updates));

        Assert.Equal(1, properties.Count);
        Assert.Equal(1, properties.Get<int>("existing"));
        Assert.False(((IPropertyCollection)properties).ContainsKey("valid"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "mutable-collection-preserves-complete-dictionary-and-bulk-contract")]
    public void MutableCollection_PreservesTheCompleteDictionaryAndBulkContract()
    {
        var properties = new JobPropertyCollection();
        properties.SetMany(
        [
            new("Tenant", "north"),
            new("attempts", 3),
        ]);
        properties.SetMany(
        [
            new("TENANT", "south"),
            new("attempts", null),
        ], overwrite: false);
        properties.Set("missing", null, overwrite: false);
        IReadOnlyDictionary<string, object> dictionary = properties;

        Assert.Equal(2, ((IReadOnlyCollection<KeyValuePair<string, object>>)properties).Count);
        Assert.Equal("north", dictionary["tenant"]);
        Assert.True(dictionary.TryGetValue("ATTEMPTS", out object? attempts));
        Assert.Equal(3, attempts);
        Assert.Equal(["attempts", "Tenant"], dictionary.Keys.Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal([3, "north"], dictionary.Values.OrderBy(static value => value.ToString(), StringComparer.Ordinal));
        Assert.Equal(2, properties.Count());
        Assert.Equal(2, ((System.Collections.IEnumerable)properties).Cast<object>().Count());

        properties.SetMany([new("ATTEMPTS", null)]);
        Assert.False(dictionary.ContainsKey("attempts"));
        Assert.Equal("properties", Assert.Throws<ArgumentNullException>(() => properties.SetMany(null!)).ParamName);
        Assert.Throws<ArgumentException>(() => _ = dictionary[" "]);
        Assert.Throws<ArgumentException>(() => dictionary.TryGetValue(" ", out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "missing-keys-are-rejected")]
    public void KeyedOperations_RejectMissingKeys(string? key)
    {
        var properties = new JobPropertyCollection();

        Assert.ThrowsAny<ArgumentException>(() => properties.Set(key!, 1));
        Assert.ThrowsAny<ArgumentException>(() => properties.TryGet(key!, out _));
        Assert.ThrowsAny<ArgumentException>(() => properties.Get<string>(key!));
        Assert.ThrowsAny<ArgumentException>(() => ((IPropertyCollection)properties).ContainsKey(key!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "public-options-expose-only-the-metadata-contract")]
    public void JobOptions_ExposeTheMetadataContractWithoutTheStorageType()
    {
        var options = new JobOptions<TestJob>();

        Assert.IsAssignableFrom<ISetPropertyCollection>(options.JobTypeProperties);
        Assert.IsAssignableFrom<ISetPropertyCollection>(options.InstanceProperties);
        Assert.Equal(typeof(ISetPropertyCollection), typeof(JobOptions<TestJob>).GetProperty(nameof(JobOptions<TestJob>.JobTypeProperties))!.PropertyType);
        Assert.Equal(typeof(ISetPropertyCollection), typeof(JobOptions<TestJob>).GetProperty(nameof(JobOptions<TestJob>.InstanceProperties))!.PropertyType);
        Assert.False(typeof(JobPropertyCollection).IsPublic);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-METADATA", "read-only-snapshot-preserves-complete-dictionary-contract")]
    public void ReadOnlySnapshot_IsCaseInsensitiveCompleteAndNotMutationCapable()
    {
        KeyValuePair<string, object>[] source =
        [
            new("Tenant", "north"),
            new("TENANT", "south"),
            new("attempts", 3),
        ];

        var snapshot = new ReadOnlyJobPropertyCollection(source);

        Assert.Equal(2, snapshot.Count);
        Assert.Equal("south", snapshot["tenant"]);
        Assert.True(snapshot.ContainsKey("TENANT"));
        Assert.True(snapshot.TryGet("tenant", out object? tenant));
        Assert.Equal("south", tenant);
        Assert.True(snapshot.TryGetValue("ATTEMPTS", out object? attempts));
        Assert.Equal(3, attempts);
        Assert.Equal(3, snapshot.Get<int>("attempts"));
        Assert.Equal("fallback", snapshot.Get<string>("missing", "fallback"));
        Assert.Equal(["attempts", "Tenant"], snapshot.Keys.Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal([3, "south"], snapshot.Values.OrderBy(static value => value.ToString(), StringComparer.Ordinal));
        Assert.Equal(2, snapshot.Count());
        Assert.IsNotAssignableFrom<ISetPropertyCollection>(snapshot);

        Assert.Throws<ArgumentException>(() => new ReadOnlyJobPropertyCollection([new KeyValuePair<string, object>(" ", 1)]));
    }

    public sealed record TestJob;
}
