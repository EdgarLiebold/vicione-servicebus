using System.Collections.Concurrent;
using System.Reflection;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusTopologyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-NAME", "deterministic-65-bit-suffix-and-provider-limit")]
    public void LongSubscriptionNames_UseTheCompleteBudgetAndACollisionResistantSuffix()
    {
        var topology = new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology());
        string prefix = new('a', 80);
        string first = topology.FormatSubscriptionName($"{prefix}-first");
        string second = topology.FormatSubscriptionName($"{prefix}-second");

        Assert.Equal("orders", topology.FormatSubscriptionName("orders"));
        Assert.Equal(50, first.Length);
        Assert.Equal('-', first[36]);
        Assert.Matches("^a{36}-[a-z0-9]{13}$", first);
        Assert.NotEqual(first, second);
        Assert.Equal(first, topology.FormatSubscriptionName($"{prefix}-first"));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-NAME", "large-same-prefix-set-has-no-collisions")]
    public void LongSubscriptionNames_AreUniqueAcrossALargeSamePrefixSet()
    {
        var topology = new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology());
        string prefix = new('a', 80);

        string[] names = Enumerable.Range(0, 10_000)
            .Select(index => topology.FormatSubscriptionName($"{prefix}-{index:D5}"))
            .ToArray();

        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-NAME", "null-empty-and-whitespace-rejected")]
    public void SubscriptionNameFormatting_RejectsMissingNames(string? name)
    {
        var topology = new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology());
        ArgumentException formatException = Assert.ThrowsAny<ArgumentException>(() => topology.FormatSubscriptionName(name!));
        ArgumentException generateException = Assert.ThrowsAny<ArgumentException>(() => topology.GenerateSubscriptionName(name!));

        Assert.Equal("subscriptionName", formatException.ParamName);
        Assert.Equal("entityName", generateException.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-NAME", "entity-and-optional-scope-compose-before-shortening")]
    public void GeneratedSubscriptionNames_ComposeTheOptionalScopeBeforeApplyingTheLimit()
    {
        var topology = new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology());
        string entityName = new('e', 45);

        Assert.Equal(entityName, topology.GenerateSubscriptionName(entityName));
        Assert.Equal(entityName, topology.GenerateSubscriptionName(entityName, "   "));

        string scoped = topology.GenerateSubscriptionName(entityName, "tenant");
        Assert.Equal(50, scoped.Length);
        Assert.StartsWith(new string('e', 36) + "-", scoped, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "custom-reflective-formatter-is-evaluated-once-without-recursion")]
    public void CustomReflectiveFormatter_IsEvaluatedOnceAndKeepsTheCompleteTypeGraph()
    {
        IMessageTopologyConfigurator messageTopology = AzureBusFactory.CreateMessageTopology();
        var formatter = new ReflectiveEntityNameFormatter(messageTopology.EntityNameFormatter);
        messageTopology.SetEntityNameFormatter(formatter);
        IServiceBusPublishTopologyConfigurator topology = new ServiceBusPublishTopology(messageTopology);

        ServiceBusMessagePublishTopology<CustomNamedEvent> first = Assert.IsType<ServiceBusMessagePublishTopology<CustomNamedEvent>>(
            topology.GetMessageTopology<CustomNamedEvent>());
        ServiceBusMessagePublishTopology<CustomNamedEvent> second = Assert.IsType<ServiceBusMessagePublishTopology<CustomNamedEvent>>(
            topology.GetMessageTopology<CustomNamedEvent>());
        BrokerTopology brokerTopology = topology.GetPublishBrokerTopology();

        Assert.Same(first, second);
        Assert.Equal("custom.named-event", first.CreateTopicOptions.Name);
        Assert.Equal(1, formatter.CallCount<CustomNamedEvent>());
        Assert.Contains(
            brokerTopology.Topics,
            topic => topic.CreateTopicOptions.Name == "custom.named-event");
        Assert.Contains(
            brokerTopology.Topics,
            topic => topic.CreateTopicOptions.Name == formatter.FallbackName<IReflectiveEvent>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "anonymous-publish-type-is-rejected-at-provider-topology-boundary")]
    public void AnonymousPublishType_IsRejectedWithTheExactDomainReason()
    {
        object anonymousMessage = new { Value = "invalid" };
        IServiceBusPublishTopologyConfigurator topology =
            new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology());

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => topology.GetMessageTopology(anonymousMessage.GetType()));

        Assert.Equal("messageType", exception.ParamName);
        Assert.Contains("must not be anonymous types", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "consume-subscription-is-exact-and-not-expanded-to-base-interface")]
    public void ConsumeSubscription_BindsOnlyTheExplicitInterfaceToTheQueue()
    {
        var formatter = new ServiceBusMessageNameFormatter();
        IServiceBusConsumeTopologyConfigurator topology = new ServiceBusConsumeTopology(
            AzureBusFactory.CreateMessageTopology(),
            new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology()));
        var builder = new ReceiveEndpointBrokerTopologyBuilder();
        const string queueName = "input-queue";
        builder.Queue = builder.CreateQueue(new ServiceBusQueueConfigurator(queueName).GetCreateQueueOptions());

        topology.GetMessageTopology<ISecond>().Subscribe("explicit-second");
        topology.Apply(builder);
        BrokerTopology actual = builder.BuildBrokerTopology();

        string first = formatter.GetMessageName(typeof(IFirst)).ToString();
        string second = formatter.GetMessageName(typeof(ISecond)).ToString();

        Assert.Equal([second], actual.Topics.Select(x => x.CreateTopicOptions.Name).Order().ToArray());
        var subscription = Assert.Single(actual.QueueSubscriptions);
        Assert.Equal(second, subscription.Source.CreateTopicOptions.Name);
        Assert.Equal(queueName, subscription.Destination.CreateQueueOptions.Name);
        Assert.DoesNotContain(actual.Topics, x => x.CreateTopicOptions.Name == first);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "publish-hierarchy-has-complete-exact-chain")]
    public void PublishHierarchy_ContainsEveryTopicAndOnlyTheDirectInheritanceEdges()
    {
        var formatter = new ServiceBusMessageNameFormatter();
        IServiceBusPublishTopologyConfigurator topology = new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology());
        var builder = new PublishEndpointBrokerTopologyBuilder(topology);

        topology.GetMessageTopology<IThird>().Apply(builder);
        BrokerTopology actual = builder.BuildBrokerTopology();

        string first = formatter.GetMessageName(typeof(IFirst)).ToString();
        string second = formatter.GetMessageName(typeof(ISecond)).ToString();
        string third = formatter.GetMessageName(typeof(IThird)).ToString();

        Assert.Equal([first, second, third], actual.Topics.Select(x => x.CreateTopicOptions.Name).Order().ToArray());
        Assert.Equal(2, actual.TopicSubscriptions.Length);
        Assert.Contains(actual.TopicSubscriptions,
            x => x.Source.CreateTopicOptions.Name == second && x.Destination.CreateTopicOptions.Name == first);
        Assert.Contains(actual.TopicSubscriptions,
            x => x.Source.CreateTopicOptions.Name == third && x.Destination.CreateTopicOptions.Name == second);
        Assert.DoesNotContain(actual.TopicSubscriptions,
            x => x.Source.CreateTopicOptions.Name == third && x.Destination.CreateTopicOptions.Name == first);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "single-interface-has-no-spurious-edge")]
    public void SingleInterface_CreatesOneTopicAndNoSubscription()
    {
        var formatter = new ServiceBusMessageNameFormatter();
        IServiceBusPublishTopologyConfigurator topology = new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology());
        var builder = new PublishEndpointBrokerTopologyBuilder(topology);

        topology.GetMessageTopology<ISingle>().Apply(builder);
        BrokerTopology actual = builder.BuildBrokerTopology();

        Assert.Equal(formatter.GetMessageName(typeof(ISingle)).ToString(), Assert.Single(actual.Topics).CreateTopicOptions.Name);
        Assert.Empty(actual.TopicSubscriptions);
    }

    public interface ISingle;

    public interface IFirst
    {
        string Value { get; }
    }

    public interface ISecond : IFirst;

    public interface IThird : ISecond;

    public interface IReflectiveEvent;

    public sealed record CustomNamedEvent : IReflectiveEvent
    {
        public static string EventName() => "custom.named-event";
    }

    private sealed class ReflectiveEntityNameFormatter(IEntityNameFormatter fallback) : IEntityNameFormatter
    {
        readonly ConcurrentDictionary<Type, int> _calls = new();

        public string FormatEntityName<T>()
        {
            _calls.AddOrUpdate(typeof(T), 1, static (_, count) => count + 1);

            return typeof(IReflectiveEvent).IsAssignableFrom(typeof(T))
                ? typeof(T).GetMethod(nameof(CustomNamedEvent.EventName), BindingFlags.Public | BindingFlags.Static)
                    ?.Invoke(null, null) as string ?? fallback.FormatEntityName<T>()
                : fallback.FormatEntityName<T>();
        }

        public int CallCount<T>() => _calls.GetValueOrDefault(typeof(T));

        public string FallbackName<T>() => fallback.FormatEntityName<T>();
    }
}
