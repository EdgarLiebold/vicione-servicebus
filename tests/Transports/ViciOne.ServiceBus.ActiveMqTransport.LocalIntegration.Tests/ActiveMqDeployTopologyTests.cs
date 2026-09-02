namespace ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests;

using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.DeployTopologyContracts;
using ViciOne.ServiceBus.ActiveMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class ActiveMqDeployTopologyTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0442", "explicit-types-exclude-unreachable-topic-at-startup")]
    public Task ExplicitTypes_ExcludeUnreachableTopicsAtStartup() =>
        AssertDeployedTopics(
            "deploy-explicit",
            configurator =>
            {
                configurator.Publish<OrderSubmitted>();
                configurator.Publish<PackageShipped>();
            },
            [],
            [typeof(PackageEvent), typeof(CustomerEvent)]);

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0443", "explicit-types-create-included-topics-at-startup")]
    public Task ExplicitTypes_CreateIncludedTopicsAtStartup() =>
        AssertDeployedTopics(
            "deploy-explicit",
            configurator =>
            {
                configurator.Publish<OrderSubmitted>();
                configurator.Publish<PackageShipped>();
            },
            [typeof(OrderSubmitted), typeof(PackageShipped)],
            []);

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0444", "namespace-scan-excludes-marked-topic-at-startup")]
    public Task NamespaceScan_ExcludesMarkedTopicAtStartup() =>
        AssertDeployedTopics(
            "deploy-namespace",
            configurator => configurator.AddPublishMessageTypesFromNamespaceContaining<OrderSubmitted>(),
            [],
            [typeof(PackageEvent)]);

    [Fact]
    [RequirementCoverage("OBL-R0-BRK-0445", "namespace-scan-creates-every-included-topic-at-startup")]
    public Task NamespaceScan_CreatesEveryIncludedTopicAtStartup() =>
        AssertDeployedTopics(
            "deploy-namespace",
            configurator => configurator.AddPublishMessageTypesFromNamespaceContaining<OrderSubmitted>(),
            [typeof(CustomerEvent), typeof(OrderSubmitted), typeof(OrderEvent), typeof(PackageShipped)],
            []);

    private static async Task AssertDeployedTopics(
        string purpose,
        Action<IActiveMqBusFactoryConfigurator> configurePublishTopology,
        Type[] includedTypes,
        Type[] excludedTypes)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(ActiveMqBroker.OpenWireFlavor, purpose);
        Type[] contractTypes =
        [
            typeof(OrderSubmitted),
            typeof(OrderEvent),
            typeof(PackageShipped),
            typeof(PackageEvent),
            typeof(CustomerEvent),
        ];
        Dictionary<Type, string> entityNames = contractTypes.ToDictionary(
            type => type,
            type => fixture.Name(type.Name));
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.DeployPublishTopology = true;
            configurator.MessageTopology.GetMessageTopology<OrderSubmitted>()
                .SetEntityName(entityNames[typeof(OrderSubmitted)]);
            configurator.MessageTopology.GetMessageTopology<OrderEvent>()
                .SetEntityName(entityNames[typeof(OrderEvent)]);
            configurator.MessageTopology.GetMessageTopology<PackageShipped>()
                .SetEntityName(entityNames[typeof(PackageShipped)]);
            configurator.MessageTopology.GetMessageTopology<PackageEvent>()
                .SetEntityName(entityNames[typeof(PackageEvent)]);
            configurator.MessageTopology.GetMessageTopology<CustomerEvent>()
                .SetEntityName(entityNames[typeof(CustomerEvent)]);
            configurePublishTopology(configurator);
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;

            foreach (Type type in includedTypes)
            {
                string topicName = $"VirtualTopic.{entityNames[type]}";
                Assert.True(
                    await fixture.ClassicTopicExists(topicName, cancellationToken),
                    $"The included topic '{topicName}' was not deployed.");
            }

            foreach (Type type in excludedTypes)
            {
                string topicName = $"VirtualTopic.{entityNames[type]}";
                Assert.False(
                    await fixture.ClassicTopicExists(topicName, cancellationToken),
                    $"The excluded topic '{topicName}' was deployed.");
            }
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }
}
