using ViciOne.ServiceBus.AzureServiceBusTransport.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests;

public sealed class ServiceBusHostAndNameValidationTests
{
    [Theory]
    [InlineData("Endpoint=sb://my-endpoint.servicebus.windows.net;SharedAccessKeyName=key;SharedAccessKey=value", "sb://my-endpoint.servicebus.windows.net/")]
    [InlineData("SharedAccessKeyName=key;SharedAccessKey=value;Endpoint=sb://my-endpoint.servicebus.windows.net", "sb://my-endpoint.servicebus.windows.net/")]
    [InlineData("Endpoint=sb://my-endpoint.servicebus.windows.net/someNamespace;SharedAccessKeyName=key;SharedAccessKey=value", "sb://my-endpoint.servicebus.windows.net/someNamespace")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "connection-string-endpoint-order-and-scope")]
    public void ConnectionString_ProjectsTheExactServiceUri(string connectionString, string expected)
    {
        var configurator = new ServiceBusHostConfigurator(connectionString);

        Assert.Equal(new Uri(expected), configurator.Settings.ServiceUri);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENTITY-NAMES", "entity-and-subscription-boundaries")]
    public void EntityAndSubscriptionNames_EnforceProviderLimitsAndCharacters()
    {
        var entity = new ServiceBusEntityNameValidator();
        var subscription = new ServiceBusSubscriptionNameValidator();

        string entityAtLimit = new('e', 260);
        string subscriptionAtLimit = new('s', 50);

        Assert.True(entity.IsValidEntityName(entityAtLimit));
        Assert.True(subscription.IsValidEntityName(subscriptionAtLimit));
        entity.ThrowIfInvalidEntityName(entityAtLimit);
        subscription.ThrowIfInvalidEntityName(subscriptionAtLimit);

        ConfigurationException longEntity = Assert.Throws<ConfigurationException>(
            () => entity.ThrowIfInvalidEntityName(entityAtLimit + "x"));
        ConfigurationException longSubscription = Assert.Throws<ConfigurationException>(
            () => subscription.ThrowIfInvalidEntityName(subscriptionAtLimit + "x"));
        ConfigurationException invalidEntity = Assert.Throws<ConfigurationException>(
            () => entity.ThrowIfInvalidEntityName("invalid#entity"));
        ConfigurationException invalidSubscription = Assert.Throws<ConfigurationException>(
            () => subscription.ThrowIfInvalidEntityName("invalid/subscription"));

        Assert.Contains("260", longEntity.Message, StringComparison.Ordinal);
        Assert.Contains(entityAtLimit + "x", longEntity.Message, StringComparison.Ordinal);
        Assert.Contains("50", longSubscription.Message, StringComparison.Ordinal);
        Assert.Contains(subscriptionAtLimit + "x", longSubscription.Message, StringComparison.Ordinal);
        Assert.Contains("invalid#entity", invalidEntity.Message, StringComparison.Ordinal);
        Assert.Contains("invalid/subscription", invalidSubscription.Message, StringComparison.Ordinal);
    }
}
