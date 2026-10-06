using System.Collections.Concurrent;
using System.Reflection;
using Azure.Messaging.ServiceBus.Administration;
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
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-NAME", "full-destination-and-namespace-identity-is-bounded-and-case-normalized")]
    public void GeneratedSubscriptionNames_IncludeTheFullDestinationAndNamespace()
    {
        var topology = new ServiceBusPublishTopology(AzureBusFactory.CreateMessageTopology());
        string entityName = new('e', 45);

        string unscoped = topology.GenerateSubscriptionName(entityName);
        string scoped = topology.GenerateSubscriptionName(entityName, "tenant");
        Assert.Matches("^auto-[0-9a-f]{40}$", unscoped);
        Assert.Matches("^auto-[0-9a-f]{40}$", scoped);
        Assert.NotEqual(unscoped, scoped);
        Assert.Equal(scoped, topology.GenerateSubscriptionName(entityName.ToUpperInvariant(), "TENANT"));
        Assert.Equal(scoped, topology.GenerateSubscriptionName(entityName, "tenant"));
        Assert.NotEqual(topology.GenerateSubscriptionName("north/orders", "tenant"),
            topology.GenerateSubscriptionName("south/orders", "tenant"));
        Assert.NotEqual(topology.GenerateSubscriptionName("bc", "a"),
            topology.GenerateSubscriptionName("c", "ab"));
        Assert.Equal("manual-orders", topology.FormatSubscriptionName("manual-orders"));
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

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "partitioned-forwarding-queue-reuses-the-propagated-declaration")]
    public void PartitionedTopicForwarding_ReusesThePropagatedQueueDeclaration()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic(new CreateTopicOptions("partitioned-source") { EnablePartitioning = true });
        QueueHandle destination = builder.CreateQueue(new CreateQueueOptions("forward-destination"));

        builder.CreateQueueSubscription(source, destination,
            new CreateSubscriptionOptions("partitioned-source", "forward-subscription"), null, null);

        QueueHandle equivalent = builder.CreateQueue(
            new CreateQueueOptions("forward-destination") { EnablePartitioning = true });
        BrokerTopology topology = builder.BuildBrokerTopology();

        Assert.Same(destination, equivalent);
        Assert.True(Assert.Single(topology.Queues).CreateQueueOptions.EnablePartitioning);
        Assert.Equal("forward-destination", Assert.Single(topology.QueueSubscriptions).Destination.CreateQueueOptions.Name);
        ArgumentException conflict = Assert.Throws<ArgumentException>(
            () => builder.CreateQueue(new CreateQueueOptions("forward-destination")));
        Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "partitioned-forwarding-topic-reuses-the-propagated-declaration")]
    public void PartitionedTopicForwarding_ReusesThePropagatedTopicDeclaration()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic(new CreateTopicOptions("partitioned-source") { EnablePartitioning = true });
        TopicHandle destination = builder.CreateTopic(new CreateTopicOptions("forward-destination"));

        builder.CreateTopicSubscription(source, destination,
            new CreateSubscriptionOptions("partitioned-source", "forward-subscription"));

        TopicHandle equivalent = builder.CreateTopic(
            new CreateTopicOptions("forward-destination") { EnablePartitioning = true });
        BrokerTopology topology = builder.BuildBrokerTopology();

        Assert.Same(destination, equivalent);
        Assert.Equal(2, topology.Topics.Length);
        Assert.True(Assert.Single(topology.Topics, topic => topic.CreateTopicOptions.Name == "forward-destination")
            .CreateTopicOptions.EnablePartitioning);
        Assert.Equal("forward-destination", Assert.Single(topology.TopicSubscriptions).Destination.CreateTopicOptions.Name);
        ArgumentException conflict = Assert.Throws<ArgumentException>(
            () => builder.CreateTopic(new CreateTopicOptions("forward-destination")));
        Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "queue-message-size-conflict-is-not-silently-reused")]
    public void QueueMessageSizeConflict_RejectsTheSecondDeclaration()
    {
        var builder = new BrokerTopologyBuilder();
        QueueHandle first = builder.CreateQueue(
            new CreateQueueOptions("size-queue") { MaxMessageSizeInKilobytes = 1024 });

        ArgumentException conflict = Assert.Throws<ArgumentException>(() => builder.CreateQueue(
            new CreateQueueOptions("size-queue") { MaxMessageSizeInKilobytes = 2048 }));

        Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
        Assert.Same(first, builder.CreateQueue(
            new CreateQueueOptions("size-queue") { MaxMessageSizeInKilobytes = 1024 }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "topic-entity-size-conflict-is-not-silently-reused")]
    public void TopicEntitySizeConflict_RejectsTheSecondDeclaration()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle first = builder.CreateTopic(
            new CreateTopicOptions("size-topic") { MaxSizeInMegabytes = 1024 });

        ArgumentException conflict = Assert.Throws<ArgumentException>(() => builder.CreateTopic(
            new CreateTopicOptions("size-topic") { MaxSizeInMegabytes = 2048 }));

        Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
        Assert.Same(first, builder.CreateTopic(
            new CreateTopicOptions("size-topic") { MaxSizeInMegabytes = 1024 }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "topic-message-size-conflict-is-not-silently-reused")]
    public void TopicMessageSizeConflict_RejectsTheSecondDeclaration()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle first = builder.CreateTopic(
            new CreateTopicOptions("size-topic") { MaxMessageSizeInKilobytes = 1024 });

        ArgumentException conflict = Assert.Throws<ArgumentException>(() => builder.CreateTopic(
            new CreateTopicOptions("size-topic") { MaxMessageSizeInKilobytes = 2048 }));

        Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
        Assert.Same(first, builder.CreateTopic(
            new CreateTopicOptions("size-topic") { MaxMessageSizeInKilobytes = 1024 }));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "queue-settings-conflicts-are-not-silently-reused")]
    public void QueueDeclaration_RejectsEveryOtherConfiguredSettingConflict()
    {
        (string Setting, Action<CreateQueueOptions> Change)[] changes =
        [
            ("auto-delete", options => options.AutoDeleteOnIdle = TimeSpan.FromMinutes(10)),
            ("message-ttl", options => options.DefaultMessageTimeToLive = TimeSpan.FromHours(1)),
            ("duplicate-window", options => options.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(7)),
            ("batched-operations", options => options.EnableBatchedOperations = false),
            ("dead-letter-expired", options => options.DeadLetteringOnMessageExpiration = true),
            ("partitioning", options => options.EnablePartitioning = true),
            ("forward-dead-letter", options => options.ForwardDeadLetteredMessagesTo = "dead-letter-target"),
            ("forward-active", options => options.ForwardTo = "active-target"),
            ("lock-duration", options => options.LockDuration = TimeSpan.FromMinutes(2)),
            ("delivery-count", options => options.MaxDeliveryCount = 7),
            ("entity-size", options => options.MaxSizeInMegabytes = 2048),
            ("duplicate-detection", options => options.RequiresDuplicateDetection = true),
            ("session", options => options.RequiresSession = true),
            ("metadata", options => options.UserMetadata = "tenant"),
        ];

        foreach ((string setting, Action<CreateQueueOptions> change) in changes)
        {
            string name = $"queue-{setting}";
            var builder = new BrokerTopologyBuilder();
            QueueHandle first = builder.CreateQueue(new CreateQueueOptions(name));
            var changed = new CreateQueueOptions(name);
            change(changed);

            Exception? failure = Record.Exception(() => builder.CreateQueue(changed));
            Assert.True(failure is ArgumentException, $"Queue setting {setting} was silently reused.");
            Assert.Contains("settings differ", failure.Message, StringComparison.Ordinal);
            Assert.Same(first, builder.CreateQueue(new CreateQueueOptions(name)));
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "topic-settings-conflicts-are-not-silently-reused")]
    public void TopicDeclaration_RejectsEveryOtherConfiguredSettingConflict()
    {
        (string Setting, Action<CreateTopicOptions> Change)[] changes =
        [
            ("auto-delete", options => options.AutoDeleteOnIdle = TimeSpan.FromMinutes(10)),
            ("message-ttl", options => options.DefaultMessageTimeToLive = TimeSpan.FromHours(1)),
            ("duplicate-window", options => options.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(7)),
            ("batched-operations", options => options.EnableBatchedOperations = false),
            ("partitioning", options => options.EnablePartitioning = true),
            ("duplicate-detection", options => options.RequiresDuplicateDetection = true),
            ("ordering", options => options.SupportOrdering = true),
            ("metadata", options => options.UserMetadata = "tenant"),
        ];

        foreach ((string setting, Action<CreateTopicOptions> change) in changes)
        {
            string name = $"topic-{setting}";
            var builder = new BrokerTopologyBuilder();
            TopicHandle first = builder.CreateTopic(new CreateTopicOptions(name));
            var changed = new CreateTopicOptions(name);
            change(changed);

            Exception? failure = Record.Exception(() => builder.CreateTopic(changed));
            Assert.True(failure is ArgumentException, $"Topic setting {setting} was silently reused.");
            Assert.Contains("settings differ", failure.Message, StringComparison.Ordinal);
            Assert.Same(first, builder.CreateTopic(new CreateTopicOptions(name)));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "queue-status-and-authorization-conflicts-rejected")]
    public void QueueDeclaration_RejectsDifferentStatusOrAuthorization(bool authorization)
    {
        var builder = new BrokerTopologyBuilder();
        QueueHandle first = builder.CreateQueue(new CreateQueueOptions("secured-queue"));
        var changed = new CreateQueueOptions("secured-queue");
        if (authorization)
            changed.AuthorizationRules.Add(new SharedAccessAuthorizationRule("tenant-listen", [AccessRights.Listen]));
        else
            changed.Status = EntityStatus.Disabled;

        ArgumentException conflict = Assert.Throws<ArgumentException>(() => builder.CreateQueue(changed));

        Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
        Assert.Same(first, builder.CreateQueue(new CreateQueueOptions("secured-queue")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "topic-status-and-authorization-conflicts-rejected")]
    public void TopicDeclaration_RejectsDifferentStatusOrAuthorization(bool authorization)
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle first = builder.CreateTopic(new CreateTopicOptions("secured-topic"));
        var changed = new CreateTopicOptions("secured-topic");
        if (authorization)
            changed.AuthorizationRules.Add(new SharedAccessAuthorizationRule("tenant-listen", [AccessRights.Listen]));
        else
            changed.Status = EntityStatus.Disabled;

        ArgumentException conflict = Assert.Throws<ArgumentException>(() => builder.CreateTopic(changed));

        Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
        Assert.Same(first, builder.CreateTopic(new CreateTopicOptions("secured-topic")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "equivalent-authorization-reuses-declaration-and-different-rights-conflict")]
    public void AuthorizationRules_ReuseEquivalentDeclarationsAndRejectDifferentRights(bool topic)
    {
        var original = new SharedAccessAuthorizationRule("tenant-access", [AccessRights.Listen]);
        var otherKeys = new SharedAccessAuthorizationRule("other-access", [AccessRights.Listen]);
        SharedAccessAuthorizationRule Rule(AccessRights right) =>
            new(original.KeyName, original.PrimaryKey, original.SecondaryKey, [right]);

        var builder = new BrokerTopologyBuilder();
        if (topic)
        {
            var firstOptions = new CreateTopicOptions("secured-topic");
            firstOptions.AuthorizationRules.Add(original);
            TopicHandle first = builder.CreateTopic(firstOptions);
            var equivalent = new CreateTopicOptions("secured-topic");
            equivalent.AuthorizationRules.Add(Rule(AccessRights.Listen));
            var conflicting = new CreateTopicOptions("secured-topic");
            conflicting.AuthorizationRules.Add(Rule(AccessRights.Send));
            var conflictingKey = new CreateTopicOptions("secured-topic");
            SharedAccessAuthorizationRule changedKey = Rule(AccessRights.Listen);
            changedKey.PrimaryKey = otherKeys.PrimaryKey;
            conflictingKey.AuthorizationRules.Add(changedKey);

            Assert.Same(first, builder.CreateTopic(equivalent));
            Assert.Contains("settings differ", Assert.Throws<ArgumentException>(
                () => builder.CreateTopic(conflicting)).Message, StringComparison.Ordinal);
            Assert.Contains("settings differ", Assert.Throws<ArgumentException>(
                () => builder.CreateTopic(conflictingKey)).Message, StringComparison.Ordinal);
        }
        else
        {
            var firstOptions = new CreateQueueOptions("secured-queue");
            firstOptions.AuthorizationRules.Add(original);
            QueueHandle first = builder.CreateQueue(firstOptions);
            var equivalent = new CreateQueueOptions("secured-queue");
            equivalent.AuthorizationRules.Add(Rule(AccessRights.Listen));
            var conflicting = new CreateQueueOptions("secured-queue");
            conflicting.AuthorizationRules.Add(Rule(AccessRights.Send));
            var conflictingKey = new CreateQueueOptions("secured-queue");
            SharedAccessAuthorizationRule changedKey = Rule(AccessRights.Listen);
            changedKey.SecondaryKey = otherKeys.SecondaryKey;
            conflictingKey.AuthorizationRules.Add(changedKey);

            Assert.Same(first, builder.CreateQueue(equivalent));
            Assert.Contains("settings differ", Assert.Throws<ArgumentException>(
                () => builder.CreateQueue(conflicting)).Message, StringComparison.Ordinal);
            Assert.Contains("settings differ", Assert.Throws<ArgumentException>(
                () => builder.CreateQueue(conflictingKey)).Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "queue-declaration-snapshots-caller-options")]
    public void QueueDeclaration_RetainsItsOriginalIdentityAfterCallerMutatesOptions()
    {
        var builder = new BrokerTopologyBuilder();
        var options = new CreateQueueOptions("original-queue") { MaxDeliveryCount = 5 };
        QueueHandle first = builder.CreateQueue(options);

        options.Name = "renamed-queue";
        options.MaxDeliveryCount = 7;
        CreateQueueOptions exposed = Assert.IsType<QueueEntity>(first).CreateQueueOptions;
        exposed.Name = "handle-mutated-queue";
        exposed.MaxDeliveryCount = 9;

        Assert.Same(first, builder.CreateQueue(
            new CreateQueueOptions("original-queue") { MaxDeliveryCount = 5 }));
        QueueHandle second = builder.CreateQueue(
            new CreateQueueOptions("renamed-queue") { MaxDeliveryCount = 7 });
        Assert.NotSame(first, second);
        Assert.Equal(["original-queue", "renamed-queue"],
            builder.BuildBrokerTopology().Queues.Select(queue => queue.CreateQueueOptions.Name).Order().ToArray());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "topic-declaration-snapshots-caller-options")]
    public void TopicDeclaration_RetainsItsOriginalIdentityAfterCallerMutatesOptions()
    {
        var builder = new BrokerTopologyBuilder();
        var options = new CreateTopicOptions("original-topic") { SupportOrdering = true };
        TopicHandle first = builder.CreateTopic(options);

        options.Name = "renamed-topic";
        options.SupportOrdering = false;
        CreateTopicOptions exposed = Assert.IsType<TopicEntity>(first).CreateTopicOptions;
        exposed.Name = "handle-mutated-topic";
        exposed.SupportOrdering = false;

        Assert.Same(first, builder.CreateTopic(
            new CreateTopicOptions("original-topic") { SupportOrdering = true }));
        TopicHandle second = builder.CreateTopic(
            new CreateTopicOptions("renamed-topic") { SupportOrdering = false });
        Assert.NotSame(first, second);
        Assert.Equal(["original-topic", "renamed-topic"],
            builder.BuildBrokerTopology().Topics.Select(topic => topic.CreateTopicOptions.Name).Order().ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "authorization-rules-are-snapshotted-on-input-and-output")]
    public void AuthorizationRules_AreNotChangedByMutatingCallerOrExposedRules(bool topic)
    {
        var originalRule = new SharedAccessAuthorizationRule("tenant-listen", [AccessRights.Listen]);
        string originalPrimaryKey = originalRule.PrimaryKey;
        string originalSecondaryKey = originalRule.SecondaryKey;
        var replacementKeys = new SharedAccessAuthorizationRule("other-access", [AccessRights.Listen]);
        var builder = new BrokerTopologyBuilder();
        AuthorizationRule exposed;
        Func<AuthorizationRule> readStored;

        if (topic)
        {
            var options = new CreateTopicOptions("secured-topic");
            options.AuthorizationRules.Add(originalRule);
            TopicHandle handle = builder.CreateTopic(options);
            exposed = Assert.Single(Assert.IsType<TopicEntity>(handle).CreateTopicOptions.AuthorizationRules);
            readStored = () => Assert.Single(Assert.IsType<TopicEntity>(handle).CreateTopicOptions.AuthorizationRules);
        }
        else
        {
            var options = new CreateQueueOptions("secured-queue");
            options.AuthorizationRules.Add(originalRule);
            QueueHandle handle = builder.CreateQueue(options);
            exposed = Assert.Single(Assert.IsType<QueueEntity>(handle).CreateQueueOptions.AuthorizationRules);
            readStored = () => Assert.Single(Assert.IsType<QueueEntity>(handle).CreateQueueOptions.AuthorizationRules);
        }

        originalRule.KeyName = "caller-changed";
        originalRule.PrimaryKey = replacementKeys.PrimaryKey;
        originalRule.SecondaryKey = replacementKeys.SecondaryKey;
        originalRule.Rights.Clear();
        originalRule.Rights.Add(AccessRights.Send);
        exposed.KeyName = "getter-changed";
        Assert.IsType<SharedAccessAuthorizationRule>(exposed).PrimaryKey = replacementKeys.PrimaryKey;
        Assert.IsType<SharedAccessAuthorizationRule>(exposed).SecondaryKey = replacementKeys.SecondaryKey;
        exposed.Rights.Clear();
        exposed.Rights.Add(AccessRights.Send);

        SharedAccessAuthorizationRule stored = Assert.IsType<SharedAccessAuthorizationRule>(readStored());
        Assert.Equal("tenant-listen", stored.KeyName);
        Assert.Equal(originalPrimaryKey, stored.PrimaryKey);
        Assert.Equal(originalSecondaryKey, stored.SecondaryKey);
        Assert.Equal([AccessRights.Listen], stored.Rights);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "subscription-status-and-initial-rule-conflicts-rejected")]
    public void SubscriptionDeclaration_RejectsDifferentStatusOrInitialRule(bool initialRule)
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic(new CreateTopicOptions("orders"));
        SubscriptionHandle first = builder.CreateSubscription(
            topic, new CreateSubscriptionOptions("orders", "consumer"), null, null);
        var changed = new CreateSubscriptionOptions("orders", "consumer");
        CreateRuleOptions? rule = null;
        if (initialRule)
            rule = new CreateRuleOptions("orders-only", new SqlRuleFilter("sys.Label = 'orders'"));
        else
            changed.Status = EntityStatus.Disabled;

        ArgumentException conflict = Assert.Throws<ArgumentException>(
            () => builder.CreateSubscription(topic, changed, rule, null));

        Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
        Assert.Same(first, builder.CreateSubscription(
            topic, new CreateSubscriptionOptions("orders", "consumer"), null, null));
        Assert.Single(builder.BuildBrokerTopology().Subscriptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "subscription-declaration-is-stable-after-caller-and-getter-mutation")]
    public void SubscriptionDeclaration_RetainsIdentityAndRuleAfterOptionsAreMutated()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic(new CreateTopicOptions("orders"));
        var options = new CreateSubscriptionOptions("orders", "consumer") { MaxDeliveryCount = 5 };
        var rule = new CreateRuleOptions("orders-only", new SqlRuleFilter("sys.Label = 'orders'"));
        SubscriptionHandle first = builder.CreateSubscription(topic, options, rule, null);

        options.SubscriptionName = "renamed-consumer";
        options.MaxDeliveryCount = 7;
        rule.Name = "renamed-rule";
        Subscription exposed = Assert.IsType<SubscriptionEntity>(first);
        exposed.CreateSubscriptionOptions.SubscriptionName = "getter-renamed-consumer";
        exposed.Rule!.Name = "getter-renamed-rule";

        Assert.Same(first, builder.CreateSubscription(topic,
            new CreateSubscriptionOptions("orders", "consumer") { MaxDeliveryCount = 5 },
            new CreateRuleOptions("orders-only", new SqlRuleFilter("sys.Label = 'orders'")), null));
        Subscription stored = Assert.Single(builder.BuildBrokerTopology().Subscriptions);
        Assert.Equal("consumer", stored.CreateSubscriptionOptions.SubscriptionName);
        Assert.Equal(5, stored.CreateSubscriptionOptions.MaxDeliveryCount);
        Assert.Equal("orders-only", stored.Rule!.Name);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "subscription-settings-conflicts-are-not-silently-reused")]
    public void SubscriptionDeclaration_RejectsEveryOtherConfiguredSettingConflict()
    {
        (string Setting, Action<CreateSubscriptionOptions> Change)[] changes =
        [
            ("auto-delete", options => options.AutoDeleteOnIdle = TimeSpan.FromMinutes(10)),
            ("message-ttl", options => options.DefaultMessageTimeToLive = TimeSpan.FromHours(1)),
            ("batched-operations", options => options.EnableBatchedOperations = false),
            ("dead-letter-expired", options => options.DeadLetteringOnMessageExpiration = true),
            ("dead-letter-filter-error", options => options.EnableDeadLetteringOnFilterEvaluationExceptions = false),
            ("forward-dead-letter", options => options.ForwardDeadLetteredMessagesTo = "dead-letter-target"),
            ("forward-active", options => options.ForwardTo = "active-target"),
            ("lock-duration", options => options.LockDuration = TimeSpan.FromMinutes(2)),
            ("delivery-count", options => options.MaxDeliveryCount = 7),
            ("session", options => options.RequiresSession = true),
            ("metadata", options => options.UserMetadata = "tenant"),
        ];

        foreach ((string setting, Action<CreateSubscriptionOptions> change) in changes)
        {
            var builder = new BrokerTopologyBuilder();
            TopicHandle topic = builder.CreateTopic(new CreateTopicOptions("orders"));
            SubscriptionHandle first = builder.CreateSubscription(topic,
                new CreateSubscriptionOptions("orders", "consumer"), null, null);
            var changed = new CreateSubscriptionOptions("orders", "consumer");
            change(changed);

            Exception? failure = Record.Exception(() => builder.CreateSubscription(topic, changed, null, null));
            Assert.True(failure is ArgumentException, $"Subscription setting {setting} was silently reused.");
            Assert.Contains("settings differ", failure.Message, StringComparison.Ordinal);
            Assert.Same(first, builder.CreateSubscription(topic,
                new CreateSubscriptionOptions("orders", "consumer"), null, null));
            Assert.Single(builder.BuildBrokerTopology().Subscriptions);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "subscription-filter-selection-conflicts-rejected")]
    public void SubscriptionDeclaration_RejectsDifferentFilterSelection()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic(new CreateTopicOptions("orders"));
        SubscriptionHandle first = builder.CreateSubscription(topic,
            new CreateSubscriptionOptions("orders", "consumer"), null, new TrueRuleFilter());

        Assert.Same(first, builder.CreateSubscription(topic,
            new CreateSubscriptionOptions("orders", "consumer"), null, new TrueRuleFilter()));
        ArgumentException conflict = Assert.Throws<ArgumentException>(() => builder.CreateSubscription(topic,
            new CreateSubscriptionOptions("orders", "consumer"), null, new FalseRuleFilter()));
        Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
        Assert.IsType<TrueRuleFilter>(Assert.Single(builder.BuildBrokerTopology().Subscriptions).Filter);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "subscription-sql-rule-and-action-parameters-are-snapshotted")]
    public void SubscriptionRule_RetainsSqlParametersAfterCallerAndGetterMutation()
    {
        var filter = new SqlRuleFilter("sys.Label = @label");
        filter.Parameters.Add("@label", "orders");
        var action = new SqlRuleAction("SET priority = @priority");
        action.Parameters.Add("@priority", 5);
        var rule = new CreateRuleOptions("orders-only", filter) { Action = action };
        var builder = new BrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic(new CreateTopicOptions("orders"));
        builder.CreateSubscription(topic, new CreateSubscriptionOptions("orders", "consumer"), rule, null);

        filter.Parameters["@label"] = "returns";
        action.Parameters["@priority"] = 9;
        CreateRuleOptions exposed = Assert.Single(builder.BuildBrokerTopology().Subscriptions).Rule!;
        Assert.IsType<SqlRuleFilter>(exposed.Filter).Parameters["@label"] = "getter";
        Assert.IsType<SqlRuleAction>(exposed.Action).Parameters["@priority"] = 12;

        CreateRuleOptions stored = Assert.Single(builder.BuildBrokerTopology().Subscriptions).Rule!;
        Assert.Equal("orders", Assert.IsType<SqlRuleFilter>(stored.Filter).Parameters["@label"]);
        Assert.Equal(5, Assert.IsType<SqlRuleAction>(stored.Action).Parameters["@priority"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "subscription-rule-parameter-conflicts-rejected")]
    public void SubscriptionDeclaration_RejectsDifferentSqlFilterOrActionParameter(bool actionParameter)
    {
        static CreateRuleOptions Rule(string label, int priority)
        {
            var filter = new SqlRuleFilter("sys.Label = @label");
            filter.Parameters.Add("@label", label);
            var action = new SqlRuleAction("SET priority = @priority");
            action.Parameters.Add("@priority", priority);
            return new CreateRuleOptions("orders-only", filter) { Action = action };
        }

        var builder = new BrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic(new CreateTopicOptions("orders"));
        SubscriptionHandle first = builder.CreateSubscription(topic,
            new CreateSubscriptionOptions("orders", "consumer"), Rule("orders", 5), null);

        Assert.Same(first, builder.CreateSubscription(topic,
            new CreateSubscriptionOptions("orders", "consumer"), Rule("orders", 5), null));
        CreateRuleOptions conflicting = actionParameter ? Rule("orders", 7) : Rule("returns", 5);
        ArgumentException conflict = Assert.Throws<ArgumentException>(() => builder.CreateSubscription(topic,
            new CreateSubscriptionOptions("orders", "consumer"), conflicting, null));
        Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
        Assert.Single(builder.BuildBrokerTopology().Subscriptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "subscription-correlation-filter-properties-are-snapshotted")]
    public void SubscriptionFilter_RetainsCorrelationPropertiesAfterCallerAndGetterMutation()
    {
        var filter = new CorrelationRuleFilter { CorrelationId = "original" };
        filter.ApplicationProperties.Add("tenant", "north");
        var builder = new BrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic(new CreateTopicOptions("orders"));
        builder.CreateSubscription(topic, new CreateSubscriptionOptions("orders", "consumer"), null, filter);

        filter.CorrelationId = "caller-changed";
        filter.ApplicationProperties["tenant"] = "south";
        var exposed = Assert.IsType<CorrelationRuleFilter>(Assert.Single(builder.BuildBrokerTopology().Subscriptions).Filter);
        exposed.CorrelationId = "getter-changed";
        exposed.ApplicationProperties["tenant"] = "east";

        var stored = Assert.IsType<CorrelationRuleFilter>(Assert.Single(builder.BuildBrokerTopology().Subscriptions).Filter);
        Assert.Equal("original", stored.CorrelationId);
        Assert.Equal("north", stored.ApplicationProperties["tenant"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "subscription-topic-handle-must-match-sdk-options")]
    public void SubscriptionDeclaration_RejectsOptionsForAnotherTopic(int relationship)
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic(new CreateTopicOptions("orders"));
        QueueHandle queue = builder.CreateQueue(new CreateQueueOptions("queue"));
        TopicHandle destination = builder.CreateTopic(new CreateTopicOptions("destination"));
        var mismatched = new CreateSubscriptionOptions("returns", "consumer");

        ArgumentException failure = Assert.Throws<ArgumentException>(() =>
        {
            if (relationship == 0)
                builder.CreateSubscription(source, mismatched, null, null);
            else if (relationship == 1)
                builder.CreateQueueSubscription(source, queue, mismatched, null, null);
            else
                builder.CreateTopicSubscription(source, destination, mismatched);
        });

        Assert.Equal("createSubscriptionOptions", failure.ParamName);
        BrokerTopology topology = builder.BuildBrokerTopology();
        Assert.Empty(topology.Subscriptions);
        Assert.Empty(topology.QueueSubscriptions);
        Assert.Empty(topology.TopicSubscriptions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "forwarding-subscription-name-cannot-target-two-destinations")]
    public void ForwardingSubscription_RejectsSameBrokerNameForDifferentDestinations(bool topicDestination)
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic(new CreateTopicOptions("orders"));
        var first = new CreateSubscriptionOptions("orders", "forward") { ForwardTo = "first" };
        var second = new CreateSubscriptionOptions("orders", "forward") { ForwardTo = "second" };

        if (topicDestination)
        {
            TopicHandle firstDestination = builder.CreateTopic(new CreateTopicOptions("first"));
            TopicHandle secondDestination = builder.CreateTopic(new CreateTopicOptions("second"));
            TopicSubscriptionHandle firstHandle = builder.CreateTopicSubscription(source, firstDestination, first);

            ArgumentException conflict = Assert.Throws<ArgumentException>(
                () => builder.CreateTopicSubscription(source, secondDestination, second));
            Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
            Assert.Same(firstHandle, builder.CreateTopicSubscription(source, firstDestination,
                new CreateSubscriptionOptions("orders", "forward") { ForwardTo = "first" }));
            Assert.Single(builder.BuildBrokerTopology().TopicSubscriptions);
        }
        else
        {
            QueueHandle firstDestination = builder.CreateQueue(new CreateQueueOptions("first"));
            QueueHandle secondDestination = builder.CreateQueue(new CreateQueueOptions("second"));
            QueueSubscriptionHandle firstHandle = builder.CreateQueueSubscription(source, firstDestination, first, null, null);

            ArgumentException conflict = Assert.Throws<ArgumentException>(
                () => builder.CreateQueueSubscription(source, secondDestination, second, null, null));
            Assert.Contains("settings differ", conflict.Message, StringComparison.Ordinal);
            Assert.Same(firstHandle, builder.CreateQueueSubscription(source, firstDestination,
                new CreateSubscriptionOptions("orders", "forward") { ForwardTo = "first" }, null, null));
            Assert.Single(builder.BuildBrokerTopology().QueueSubscriptions);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "subscription-broker-name-is-unique-across-relationship-kinds")]
    public void SubscriptionDeclaration_RejectsBrokerNameAlreadyUsedByAnotherRelationshipKind()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic(new CreateTopicOptions("orders"));
        QueueHandle destination = builder.CreateQueue(new CreateQueueOptions("forward-queue"));
        SubscriptionHandle first = builder.CreateSubscription(source,
            new CreateSubscriptionOptions("orders", "consumer"), null, null);

        ArgumentException conflict = Assert.Throws<ArgumentException>(() => builder.CreateQueueSubscription(
            source, destination,
            new CreateSubscriptionOptions("orders", "consumer") { ForwardTo = "forward-queue" }, null, null));

        Assert.Contains("already declared", conflict.Message, StringComparison.Ordinal);
        Assert.Same(first, Assert.Single(builder.BuildBrokerTopology().Subscriptions));
        Assert.Empty(builder.BuildBrokerTopology().QueueSubscriptions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "forwarding-target-must-match-destination-handle")]
    public void ForwardingSubscription_RejectsAnotherForwardingTargetWithoutPromotingDestination(bool topicDestination)
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic(new CreateTopicOptions("orders") { EnablePartitioning = true });
        var options = new CreateSubscriptionOptions("orders", "forward") { ForwardTo = "wrong-target" };

        if (topicDestination)
        {
            TopicHandle destination = builder.CreateTopic(new CreateTopicOptions("target"));
            ArgumentException failure = Assert.Throws<ArgumentException>(
                () => builder.CreateTopicSubscription(source, destination, options));
            Assert.Equal("createSubscriptionOptions", failure.ParamName);
            Assert.False(Assert.Single(builder.BuildBrokerTopology().Topics,
                topic => topic.CreateTopicOptions.Name == "target").CreateTopicOptions.EnablePartitioning);
            Assert.Empty(builder.BuildBrokerTopology().TopicSubscriptions);
        }
        else
        {
            QueueHandle destination = builder.CreateQueue(new CreateQueueOptions("target"));
            ArgumentException failure = Assert.Throws<ArgumentException>(
                () => builder.CreateQueueSubscription(source, destination, options, null, null));
            Assert.Equal("createSubscriptionOptions", failure.ParamName);
            Assert.False(Assert.Single(builder.BuildBrokerTopology().Queues).CreateQueueOptions.EnablePartitioning);
            Assert.Empty(builder.BuildBrokerTopology().QueueSubscriptions);
        }
    }

    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "")]
    [InlineData(false, " ")]
    [InlineData(true, null)]
    [InlineData(true, "")]
    [InlineData(true, " ")]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "forwarding-target-defaults-to-destination-without-caller-mutation")]
    public void ForwardingSubscription_FillsOmittedTargetFromDestinationWithoutMutatingCaller(bool topicDestination, string? omittedTarget)
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic(new CreateTopicOptions("orders"));
        var options = new CreateSubscriptionOptions("orders", "forward") { ForwardTo = omittedTarget };

        if (topicDestination)
        {
            TopicHandle destination = builder.CreateTopic(new CreateTopicOptions("target"));
            TopicSubscriptionHandle first = builder.CreateTopicSubscription(source, destination, options);
            Assert.Same(first, builder.CreateTopicSubscription(source, destination,
                new CreateSubscriptionOptions("orders", "forward")));
            Assert.Equal("target", Assert.Single(builder.BuildBrokerTopology().TopicSubscriptions)
                .Subscription.CreateSubscriptionOptions.ForwardTo);
        }
        else
        {
            QueueHandle destination = builder.CreateQueue(new CreateQueueOptions("target"));
            QueueSubscriptionHandle first = builder.CreateQueueSubscription(source, destination, options, null, null);
            Assert.Same(first, builder.CreateQueueSubscription(source, destination,
                new CreateSubscriptionOptions("orders", "forward"), null, null));
            Assert.Equal("target", Assert.Single(builder.BuildBrokerTopology().QueueSubscriptions)
                .Subscription.CreateSubscriptionOptions.ForwardTo);
        }

        Assert.Equal(omittedTarget, options.ForwardTo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "partitioning-propagates-through-existing-forwarding-relationships")]
    public void ForwardingSubscription_PropagatesLatePartitioningThroughExistingRelationships(bool topicDestination)
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle root = builder.CreateTopic(new CreateTopicOptions("root") { EnablePartitioning = true });
        TopicHandle source = builder.CreateTopic(new CreateTopicOptions("source"));

        if (topicDestination)
        {
            TopicHandle destination = builder.CreateTopic(new CreateTopicOptions("target"));
            builder.CreateTopicSubscription(source, destination, new CreateSubscriptionOptions("source", "forward"));
            builder.CreateTopicSubscription(root, source, new CreateSubscriptionOptions("root", "promote-source"));

            Assert.True(Assert.Single(builder.BuildBrokerTopology().Topics,
                topic => topic.CreateTopicOptions.Name == "source").CreateTopicOptions.EnablePartitioning);
            Assert.True(Assert.Single(builder.BuildBrokerTopology().Topics,
                topic => topic.CreateTopicOptions.Name == "target").CreateTopicOptions.EnablePartitioning);
        }
        else
        {
            QueueHandle destination = builder.CreateQueue(new CreateQueueOptions("target"));
            builder.CreateQueueSubscription(source, destination, new CreateSubscriptionOptions("source", "forward"), null, null);
            builder.CreateTopicSubscription(root, source, new CreateSubscriptionOptions("root", "promote-source"));

            Assert.True(Assert.Single(builder.BuildBrokerTopology().Topics,
                topic => topic.CreateTopicOptions.Name == "source").CreateTopicOptions.EnablePartitioning);
            Assert.True(Assert.Single(builder.BuildBrokerTopology().Queues).CreateQueueOptions.EnablePartitioning);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "session-enabled-subscriptions-cannot-be-forwarding-sources")]
    public void ForwardingSubscription_RejectsSessionEnabledSourceBeforeChangingTopology(bool topicDestination)
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic(new CreateTopicOptions("orders") { EnablePartitioning = true });
        var options = new CreateSubscriptionOptions("orders", "forward") { RequiresSession = true };

        if (topicDestination)
        {
            TopicHandle destination = builder.CreateTopic(new CreateTopicOptions("target"));
            ArgumentException failure = Assert.Throws<ArgumentException>(
                () => builder.CreateTopicSubscription(source, destination, options));
            Assert.Equal("createSubscriptionOptions", failure.ParamName);
            Assert.Empty(builder.BuildBrokerTopology().TopicSubscriptions);
            Assert.False(Assert.Single(builder.BuildBrokerTopology().Topics,
                topic => topic.CreateTopicOptions.Name == "target").CreateTopicOptions.EnablePartitioning);
        }
        else
        {
            QueueHandle destination = builder.CreateQueue(new CreateQueueOptions("target"));
            ArgumentException failure = Assert.Throws<ArgumentException>(
                () => builder.CreateQueueSubscription(source, destination, options, null, null));
            Assert.Equal("createSubscriptionOptions", failure.ParamName);
            Assert.Empty(builder.BuildBrokerTopology().QueueSubscriptions);
            Assert.False(Assert.Single(builder.BuildBrokerTopology().Queues).CreateQueueOptions.EnablePartitioning);
        }

        Assert.Null(options.ForwardTo);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "broker-entity-names-ignore-case-for-deduplication-and-conflicts")]
    public void BrokerEntities_ReuseCaseVariantNamesAndRejectCaseVariantSettingConflicts()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic(new CreateTopicOptions("Orders"));
        QueueHandle queue = builder.CreateQueue(new CreateQueueOptions("Target"));

        Assert.Same(topic, builder.CreateTopic(new CreateTopicOptions("orders")));
        Assert.Same(queue, builder.CreateQueue(new CreateQueueOptions("target")));
        Assert.Throws<ArgumentException>(() => builder.CreateTopic(
            new CreateTopicOptions("ORDERS") { SupportOrdering = true }));
        Assert.Throws<ArgumentException>(() => builder.CreateQueue(
            new CreateQueueOptions("TARGET") { MaxDeliveryCount = 7 }));
        Assert.Single(builder.BuildBrokerTopology().Topics);
        Assert.Single(builder.BuildBrokerTopology().Queues);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "subscription-identity-ignores-case-across-relationship-kinds")]
    public void SubscriptionDeclaration_RejectsCaseVariantBrokerNameAcrossRelationshipKinds()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic(new CreateTopicOptions("Orders"));
        QueueHandle queue = builder.CreateQueue(new CreateQueueOptions("Target"));
        SubscriptionHandle first = builder.CreateSubscription(topic,
            new CreateSubscriptionOptions("Orders", "Consumer"), null, null);

        Assert.Same(first, builder.CreateSubscription(topic,
            new CreateSubscriptionOptions("orders", "consumer"), null, null));
        ArgumentException failure = Assert.Throws<ArgumentException>(() => builder.CreateQueueSubscription(topic, queue,
            new CreateSubscriptionOptions("ORDERS", "CONSUMER") { ForwardTo = "target" }, null, null));

        Assert.Equal("createSubscriptionOptions", failure.ParamName);
        Assert.Empty(builder.BuildBrokerTopology().QueueSubscriptions);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "forwarding-target-identity-ignores-case")]
    public void ForwardingSubscription_AcceptsCaseVariantOfDestinationName()
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle source = builder.CreateTopic(new CreateTopicOptions("Orders"));
        QueueHandle destination = builder.CreateQueue(new CreateQueueOptions("Target"));
        var options = new CreateSubscriptionOptions("orders", "forward") { ForwardTo = "target" };

        QueueSubscriptionHandle first = builder.CreateQueueSubscription(source, destination, options, null, null);
        Assert.Same(first, builder.CreateQueueSubscription(source, destination,
            new CreateSubscriptionOptions("ORDERS", "FORWARD") { ForwardTo = "TARGET" }, null, null));
        Assert.Equal("Target", Assert.Single(builder.BuildBrokerTopology().QueueSubscriptions)
            .Subscription.CreateSubscriptionOptions.ForwardTo);
        Assert.Equal("target", options.ForwardTo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-TOPOLOGY", "direct-session-enabled-entities-cannot-auto-forward")]
    public void DirectEntityDeclaration_RejectsSessionEnabledAutoForwarding(bool queueEntity)
    {
        var builder = new BrokerTopologyBuilder();
        TopicHandle topic = builder.CreateTopic(new CreateTopicOptions("orders"));

        if (queueEntity)
        {
            var options = new CreateQueueOptions("source") { RequiresSession = true, ForwardTo = "destination" };
            ArgumentException failure = Assert.Throws<ArgumentException>(() => builder.CreateQueue(options));
            Assert.Equal("createQueueOptions", failure.ParamName);
            Assert.Empty(builder.BuildBrokerTopology().Queues);
        }
        else
        {
            var options = new CreateSubscriptionOptions("orders", "source")
            {
                RequiresSession = true,
                ForwardTo = "destination"
            };
            ArgumentException failure = Assert.Throws<ArgumentException>(
                () => builder.CreateSubscription(topic, options, null, null));
            Assert.Equal("createSubscriptionOptions", failure.ParamName);
            Assert.Empty(builder.BuildBrokerTopology().Subscriptions);
        }
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
