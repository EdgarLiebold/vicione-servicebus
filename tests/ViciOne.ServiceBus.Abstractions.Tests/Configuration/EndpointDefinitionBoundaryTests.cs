using System.Reflection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Configuration;

public sealed class EndpointDefinitionBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEFINITION", "settings-validation")]
    public void EndpointSettings_RejectInvalidNamesLimitsAndCallbacks()
    {
        var settings = new EndpointSettings<IEndpointDefinition<TestConsumer>>();

        Assert.Equal("value", Assert.Throws<ArgumentException>(() => settings.Name = " ").ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentException>(() => settings.InstanceId = " ").ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => settings.PrefetchCount = -1).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => settings.ConcurrentMessageLimit = 0).ParamName);
        Assert.Equal(
            "callback",
            Assert.Throws<ArgumentNullException>(() =>
                settings.AddConfigureEndpointCallback((Action<IReceiveEndpointConfigurator>)null!)).ParamName);
        Assert.Equal(
            "callback",
            Assert.Throws<ArgumentNullException>(() =>
                settings.AddConfigureEndpointCallback((Action<IRegistrationContext, IReceiveEndpointConfigurator>)null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEFINITION", "callback-context-and-order")]
    public void EndpointSettings_InvokeCallbacksInRegistrationOrderAndRequireContextWhenUsed()
    {
        var settings = new EndpointSettings<IEndpointDefinition<TestConsumer>>();
        IReceiveEndpointConfigurator endpoint = CreateProxy<IReceiveEndpointConfigurator>();
        IRegistrationContext context = CreateProxy<IRegistrationContext>();
        var calls = new List<int>();

        settings.AddConfigureEndpointCallback(configurator =>
        {
            Assert.Same(endpoint, configurator);
            calls.Add(1);
        });
        settings.AddConfigureEndpointCallback((registrationContext, configurator) =>
        {
            Assert.Same(context, registrationContext);
            Assert.Same(endpoint, configurator);
            calls.Add(2);
        });

        settings.ConfigureEndpoint(endpoint, context);

        Assert.Equal([1, 2], calls);
        Assert.Equal(
            "configurator",
            Assert.Throws<ArgumentNullException>(() => settings.ConfigureEndpoint<IReceiveEndpointConfigurator>(null!, context)).ParamName);
        Assert.Throws<ConfigurationException>(() => settings.ConfigureEndpoint(endpoint, null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEFINITION", "definition-input-validation")]
    public void EndpointDefinitions_RejectInvalidConstructionNamingAndConfigurationInputs()
    {
        Assert.Equal(
            "tag",
            Assert.Throws<ArgumentException>(() => new TemporaryEndpointDefinition(" ")).ParamName);
        Assert.Equal(
            "concurrentMessageLimit",
            Assert.Throws<ArgumentOutOfRangeException>(() => new TemporaryEndpointDefinition(concurrentMessageLimit: 0)).ParamName);
        Assert.Equal(
            "prefetchCount",
            Assert.Throws<ArgumentOutOfRangeException>(() => new TemporaryEndpointDefinition(prefetchCount: -1)).ParamName);

        var temporary = new TemporaryEndpointDefinition();
        Assert.Equal(
            "formatter",
            Assert.Throws<ArgumentNullException>(() => temporary.GetEndpointName(null!)).ParamName);
        Assert.Equal(
            "configurator",
            Assert.Throws<ArgumentNullException>(() => temporary.Configure<IReceiveEndpointConfigurator>(null!, null)).ParamName);

        Assert.Equal(
            "settings",
            Assert.Throws<ArgumentNullException>(() => new ConsumerEndpointDefinition<TestConsumer>(null!)).ParamName);

        var definition = new ConsumerEndpointDefinition<TestConsumer>(new EndpointSettings<IEndpointDefinition<TestConsumer>>());
        Assert.Equal(
            "formatter",
            Assert.Throws<ArgumentNullException>(() => definition.GetEndpointName(null!)).ParamName);
        Assert.Equal(
            "configurator",
            Assert.Throws<ArgumentNullException>(() => definition.Configure<IReceiveEndpointConfigurator>(null!, null)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEFINITION", "consumer-definition-input-validation")]
    public void ConsumerDefinition_RejectsInvalidEndpointNamesAndMissingConfigurationDependencies()
    {
        var definition = new TestConsumerDefinition();
        IConsumerDefinition untypedDefinition = definition;
        IConsumerDefinition<TestConsumer> typedDefinition = definition;
        IReceiveEndpointConfigurator endpoint = CreateProxy<IReceiveEndpointConfigurator>();
        IConsumerConfigurator<TestConsumer> consumer = CreateProxy<IConsumerConfigurator<TestConsumer>>();
        IRegistrationContext context = CreateProxy<IRegistrationContext>();

        Assert.Equal("value", Assert.Throws<ArgumentException>(() => definition.SetEndpointName(" ")).ParamName);
        Assert.Equal(
            "formatter",
            Assert.Throws<ArgumentNullException>(() => untypedDefinition.GetEndpointName(null!)).ParamName);
        Assert.Equal(
            "endpointConfigurator",
            Assert.Throws<ArgumentNullException>(() => typedDefinition.Configure(null!, consumer, context)).ParamName);
        Assert.Equal(
            "consumerConfigurator",
            Assert.Throws<ArgumentNullException>(() => typedDefinition.Configure(endpoint, null!, context)).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => typedDefinition.Configure(endpoint, consumer, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ENDPOINT-DEFINITION", "descriptive-public-metadata")]
    public void EndpointDefinitionContracts_ExposeDescriptiveGenericParameterNames()
    {
        Assert.Equal("TRegistration", Assert.Single(typeof(IEndpointDefinition<>).GetGenericArguments()).Name);
        Assert.Equal("TDefinition", Assert.Single(typeof(IEndpointSettings<>).GetGenericArguments()).Name);
        Assert.Equal("TDefinition", Assert.Single(typeof(EndpointSettings<>).GetGenericArguments()).Name);
        Assert.Equal("TRegistration", Assert.Single(typeof(EndpointRegistrationConfigurator<>).GetGenericArguments()).Name);
        Assert.Equal("TRegistration", Assert.Single(typeof(SettingsEndpointDefinition<>).GetGenericArguments()).Name);

        MethodInfo configureDefinition = Assert.Single(typeof(IEndpointDefinition).GetMethods(), method => method.Name == "Configure");
        Assert.Equal("TEndpointConfigurator", Assert.Single(configureDefinition.GetGenericArguments()).Name);

        MethodInfo configureConsumer = Assert.Single(typeof(IRegistrationContext).GetMethods(), method =>
            method.Name == nameof(IRegistrationContext.ConfigureConsumer)
            && method.IsGenericMethod);
        Assert.Equal("TConsumer", Assert.Single(configureConsumer.GetGenericArguments()).Name);
    }

    private static T CreateProxy<T>()
        where T : class => DispatchProxy.Create<T, RejectInvocationProxy>();

    private sealed class TestConsumerDefinition : ConsumerDefinition<TestConsumer>
    {
        public void SetEndpointName(string value) => EndpointName = value;
    }

    private sealed class TestConsumer : IConsumer;

    private class RejectInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected proxy invocation: {targetMethod?.Name}");
    }
}
