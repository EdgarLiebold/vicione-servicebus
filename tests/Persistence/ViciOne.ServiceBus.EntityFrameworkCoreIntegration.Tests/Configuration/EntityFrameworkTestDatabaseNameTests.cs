using ViciOne.ServiceBus.Tests.Infrastructure.Databases;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration.Tests.Configuration;

public sealed class EntityFrameworkTestDatabaseNameTests
{
    public static TheoryData<string> ProviderVariants => new()
    {
        "PostgresTestDbParameters",
        "SqlServerTestDbParameters",
        "SqlServerResiliencyTestDbParameters",
    };

    [Theory]
    [MemberData(nameof(ProviderVariants))]
    [RequirementCoverage("REQ-VSB-EF-TEST-DATABASE-IDENTITY", "setup-and-teardown-share-run-name")]
    public void RunIdentity_ProducesOneProviderSafeNameForSetupAndTeardown(string providerVariant)
    {
        const string runIdentity = "test-case/018f_\u00c4:parallel-7";

        string setupName = TestDatabaseName.Create(providerVariant, runIdentity);
        string teardownName = TestDatabaseName.Create(providerVariant, runIdentity);

        Assert.Equal(setupName, teardownName);
        Assert.Matches("^[a-z0-9]+_[a-f0-9]{32}$", setupName);
        Assert.InRange(setupName.Length, 34, 63);
    }

    [Theory]
    [MemberData(nameof(ProviderVariants))]
    [RequirementCoverage("REQ-VSB-EF-TEST-DATABASE-IDENTITY", "distinct-runs-never-use-fixed-fallback")]
    public void DistinctRunIdentities_ProduceDistinctNamesWithoutAFixedFallback(string providerVariant)
    {
        string first = TestDatabaseName.Create(providerVariant, "run-a");
        string second = TestDatabaseName.Create(providerVariant, "run-b");

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("masstransit", first, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("servicebus_test", first, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(TestDatabaseName.Create(providerVariant, providerVariant), first);
    }
}
