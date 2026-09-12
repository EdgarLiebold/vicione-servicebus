using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorFactoryContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-FACTORY", "default-and-custom-base-address")]
    public async Task Create_UsesTheExactDefaultOrCustomBaseAddressAsync()
    {
        await using IMediator defaultMediator = CreateMediator();
        await using IMediator customMediator = CreateMediator(new Uri("loopback://mediator-host/application"));

        Assert.Equal(new Uri("loopback://localhost/response"), defaultMediator.Context.ResponseAddress);
        Assert.Equal(new Uri("loopback://mediator-host/application/response"), customMediator.Context.ResponseAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-FACTORY", "null-configuration-callback")]
    public void Create_RejectsANullConfigurationCallback()
    {
        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(() =>
            MediatorFactory.Create(null!));

        Assert.Equal("configure", failure.ParamName);
    }

    [Theory]
    [InlineData("relative-base")]
    [InlineData("memory://localhost/")]
    [InlineData("loopback:///application")]
    [InlineData("loopback://user@localhost/application")]
    [InlineData("loopback://localhost:1234/application")]
    [InlineData("loopback://localhost/application?option=value")]
    [InlineData("loopback://localhost/application#fragment")]
    [RequirementCoverage("REQ-VSB-MEDIATOR-FACTORY", "invalid-base-address-boundaries")]
    public void Create_RejectsInvalidBaseAddressesBeforeInvokingConfiguration(string value)
    {
        var invocations = 0;
        var address = new Uri(value, UriKind.RelativeOrAbsolute);

        ArgumentException failure = Assert.Throws<ArgumentException>(() =>
            MediatorFactory.Create(
                configuration =>
                {
                    Interlocked.Increment(ref invocations);
                    configuration.Limits(MessageLimits.Conservative);
                },
                address));

        Assert.Equal("baseAddress", failure.ParamName);
        Assert.Equal(0, Volatile.Read(ref invocations));
    }

    [Theory]
    [InlineData("relative-base")]
    [InlineData("memory://localhost/")]
    [InlineData("loopback://localhost/application?option=value")]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DI", "invalid-base-address-fails-at-registration")]
    public void AddMediator_RejectsInvalidBaseAddressesBeforeChangingTheServiceCollection(string value)
    {
        var services = new ServiceCollection();
        var address = new Uri(value, UriKind.RelativeOrAbsolute);

        ArgumentException failure = Assert.Throws<ArgumentException>(() =>
            services.AddMediator(address, configuration => configuration.Limits(MessageLimits.Conservative)));

        Assert.Equal("baseAddress", failure.ParamName);
        Assert.Empty(services);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-DI", "duplicate-registration-is-rejected")]
    public void AddMediator_RejectsDuplicateRegistrationWithoutReplacingTheFirst()
    {
        var services = new ServiceCollection();
        services.AddMediator(configuration => configuration.Limits(MessageLimits.Conservative));
        int descriptorCount = services.Count;

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            services.AddMediator(configuration => configuration.Limits(MessageLimits.Conservative)));

        Assert.Contains("already called", failure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(descriptorCount, services.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-CONFIGURATION", "direct-duplicate-limits-is-rejected")]
    public void DirectConfiguration_RejectsASecondLimitsPolicyEvenWhenItIsTheSameInstance()
    {
        MessageLimits limits = MessageLimits.Conservative;

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            MediatorFactory.Create(configuration =>
            {
                configuration.Limits(limits);
                configuration.Limits(limits);
            }));

        Assert.Contains("already declared", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-CONFIGURATION", "container-duplicate-limits-is-rejected")]
    public void ContainerConfiguration_RejectsASecondLimitsPolicy()
    {
        var services = new ServiceCollection();

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            services.AddMediator(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.Limits(new MessageLimits
                {
                    MaxBodyBytes = 4096,
                    MaxEnvelopeBytes = 4096,
                    MaxJsonDepth = 16,
                });
            }));

        Assert.Contains("already declared", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-CONFIGURATION", "duplicate-materialization-callback-is-rejected")]
    public void ConfigureMediator_RejectsASecondMaterializationCallback()
    {
        var services = new ServiceCollection();

        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            services.AddMediator(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.ConfigureMediator((_, _) => { });
                configuration.ConfigureMediator((_, _) => { });
            }));

        Assert.Contains("ConfigureMediator", failure.Message, StringComparison.Ordinal);
        Assert.Contains("only once", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-GREENFIELD-API", "single-explicit-factory-entry-point")]
    public void Assembly_ExposesTheGreenfieldFactoryWithoutLegacyFactoryOrHostBuilderAdapters()
    {
        Assembly assembly = typeof(IMediator).Assembly;
        MethodInfo create = Assert.Single(typeof(MediatorFactory).GetMethods(BindingFlags.Public | BindingFlags.Static));

        Assert.Equal(nameof(MediatorFactory.Create), create.Name);
        Assert.Equal(typeof(IMediator), create.ReturnType);
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Configuration.MediatorConfigurationExtensions"));
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Configuration.MediatorHostBuilderExtensions"));
    }

    private static IMediator CreateMediator(Uri? baseAddress = null) => MediatorFactory.Create(
        configuration => configuration.Limits(MessageLimits.Conservative),
        baseAddress);
}
