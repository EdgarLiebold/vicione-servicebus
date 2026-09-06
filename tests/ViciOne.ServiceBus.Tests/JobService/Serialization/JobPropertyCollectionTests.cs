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

    public sealed record TestJob;
}
