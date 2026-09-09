using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Providers.Configuration;
using ViciOne.ServiceBus.Providers.Persistence;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Configuration;

public sealed class ConfigurationBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OBSERVATION", "endpoint-configurator-required")]
    public void EndpointObservation_RejectsAMissingConfigurator()
    {
        var observable = new EndpointConfigurationObservable();

        Assert.Equal(
            "configurator",
            Assert.Throws<ArgumentNullException>(() =>
                observable.EndpointConfigured<IReceiveEndpointConfigurator>(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PERSISTENCE-IDENTITY", "feature-name-required")]
    public void PersistenceIdentity_RequiresAFeatureNameEvenWhenTheIdentityIsAvailable()
    {
        BusPersistenceIdentity<IBus> identity = BusPersistenceIdentity<IBus>.Create("orders");

        Assert.Equal(
            "feature",
            Assert.Throws<ArgumentException>(() => identity.Require(" ")).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-MESSAGES", "aggregate-validates-and-preserves-order")]
    public void ConfigurationFailureAggregation_ValidatesEntriesAndPreservesFirstOccurrenceOrder()
    {
        Assert.Equal(
            $"first{Environment.NewLine}second",
            ConfigurationMessages.Aggregate(["first", "first", "second"]));
        Assert.Equal(
            "failures",
            Assert.Throws<ArgumentNullException>(() => ConfigurationMessages.Aggregate(null!)).ParamName);
        Assert.Equal(
            "failure",
            Assert.Throws<ArgumentException>(() => ConfigurationMessages.Aggregate(["valid", " "])).ParamName);
        Assert.Equal(
            "failure",
            Assert.Throws<ArgumentNullException>(() => ConfigurationMessages.Aggregate(["valid", null!])).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONFIGURATION-OPTIONS", "identity-null-and-type-selection")]
    public void OptionsSet_PreservesIdentityRejectsNullAndSelectsAssignableOptions()
    {
        var set = new OptionsSet();

        TestOptions first = set.Options<TestOptions>(options => options.ConfigurationCount++);
        TestOptions second = set.Options<TestOptions>(options => options.ConfigurationCount++);

        Assert.Same(first, second);
        Assert.Equal(2, first.ConfigurationCount);
        Assert.Same(first, set.Options(first));
        Assert.True(set.TryGetOptions(out TestOptions selected));
        Assert.Same(first, selected);
        Assert.Equal([first], set.SelectOptions<IOptions>());
        Assert.Throws<ArgumentException>(() => set.Options(new TestOptions()));
        Assert.Equal(
            "options",
            Assert.Throws<ArgumentNullException>(() => set.Options((TestOptions)null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONFIGURATION", "intentional-public-metadata")]
    public void BatchOptions_ExposeOnlyIntentionalStateAndDescriptiveGenericAndDurationNames()
    {
        Assert.Null(typeof(BatchOptions).GetProperty("GroupKeyProvider", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(BatchOptions).GetProperty("GroupKeyProvider", BindingFlags.Instance | BindingFlags.NonPublic));

        MethodInfo durationOverload = Assert.Single(typeof(BatchOptions).GetMethods(), method =>
            method.Name == nameof(BatchOptions.SetTimeLimit)
            && method.GetParameters().Length == 5);
        Assert.Equal(
            ["milliseconds", "seconds", "minutes", "hours", "days"],
            durationOverload.GetParameters().Select(parameter => parameter.Name));

        MethodInfo[] groupByOverloads = typeof(BatchOptions).GetMethods()
            .Where(method => method.Name == nameof(BatchOptions.GroupBy))
            .ToArray();
        Assert.Equal(2, groupByOverloads.Length);
        Assert.All(groupByOverloads, method =>
            Assert.Equal(["TMessage", "TKey"], method.GetGenericArguments().Select(argument => argument.Name)));
    }

    private sealed class TestOptions : IOptions
    {
        public int ConfigurationCount { get; set; }
    }
}
