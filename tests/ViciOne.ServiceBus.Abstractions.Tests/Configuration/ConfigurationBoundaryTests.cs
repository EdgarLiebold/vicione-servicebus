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
    public void BatchOptions_ExposeOnlyIntentionalStateAndDescriptiveGroupingMethods()
    {
        Assert.Null(typeof(BatchOptions).GetProperty("GroupKeyProvider", BindingFlags.Instance | BindingFlags.Public));
        Assert.NotNull(typeof(BatchOptions).GetProperty("GroupKeyProvider", BindingFlags.Instance | BindingFlags.NonPublic));

        MethodInfo timeLimitMethod = Assert.Single(typeof(BatchOptions).GetMethods(), method =>
            method.Name == nameof(BatchOptions.SetTimeLimit));
        ParameterInfo timeLimit = Assert.Single(timeLimitMethod.GetParameters());
        Assert.Equal("limit", timeLimit.Name);
        Assert.Equal(typeof(TimeSpan), timeLimit.ParameterType);

        MethodInfo[] groupByOverloads = typeof(BatchOptions).GetMethods()
            .Where(method => method.Name == nameof(BatchOptions.GroupBy))
            .ToArray();
        Assert.Equal(2, groupByOverloads.Length);
        Assert.All(groupByOverloads, method =>
            Assert.Equal(["TMessage", "TKey"], method.GetGenericArguments().Select(argument => argument.Name)));

        Assert.False(typeof(IGroupKeyProvider<,>).IsPublic);
        Assert.False(typeof(GroupKeyProvider<,>).IsPublic);
        Assert.False(typeof(ValueTypeGroupKeyProvider<,>).IsPublic);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BATCH-CONFIGURATION", "grouping-adapter-boundaries-and-selection")]
    public void GroupingAdapters_ValidateInputsAndPreserveOptionalKeySemantics()
    {
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new ValueTypeGroupKeyProvider<TestBatchMessage, int>(null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new GroupKeyProvider<TestBatchMessage, string>(null!)).ParamName);

        var valueProvider = new ValueTypeGroupKeyProvider<TestBatchMessage, int>(context =>
            context.Message.Value.Length == 0 ? null : context.Message.Value.Length);
        var referenceProvider = new GroupKeyProvider<TestBatchMessage, string>(context =>
            string.IsNullOrEmpty(context.Message.Value) ? null : context.Message.Value);

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => valueProvider.TryGetKey(null!, out _)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => referenceProvider.TryGetKey(null!, out _)).ParamName);

        ConsumeContext<TestBatchMessage> populated = CreateContext(new TestBatchMessage("group"));
        ConsumeContext<TestBatchMessage> empty = CreateContext(new TestBatchMessage(string.Empty));
        Assert.True(valueProvider.TryGetKey(populated, out int valueKey));
        Assert.Equal(5, valueKey);
        Assert.False(valueProvider.TryGetKey(empty, out _));
        Assert.True(referenceProvider.TryGetKey(populated, out string? referenceKey));
        Assert.Equal("group", referenceKey);
        Assert.False(referenceProvider.TryGetKey(empty, out _));
    }

    private static ConsumeContext<TestBatchMessage> CreateContext(TestBatchMessage message)
    {
        ConsumeContext<TestBatchMessage> context = DispatchProxy.Create<ConsumeContext<TestBatchMessage>, MessageContextProxy>();
        ((MessageContextProxy)(object)context).Message = message;
        return context;
    }

    private sealed class TestOptions : IOptions
    {
        public int ConfigurationCount { get; set; }
    }

    private sealed record TestBatchMessage(string Value);

    private class MessageContextProxy : DispatchProxy
    {
        public TestBatchMessage Message { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            return targetMethod.Name == "get_Message"
                ? Message
                : throw new NotSupportedException(targetMethod.Name);
        }
    }
}
