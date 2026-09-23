using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusSubscriptionOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-OPTIONS", "configured-settings-reach-sdk-subscription-creation")]
    public void ConfiguredSettings_ReachTheSdkSubscriptionCreationBoundary()
    {
        var configurator = new ServiceBusSubscriptionConfigurator("orders-worker", "orders-topic")
        {
            AutoDeleteOnIdle = TimeSpan.FromMinutes(17),
            DefaultMessageTimeToLive = TimeSpan.FromDays(3),
            EnableBatchedOperations = false,
            EnableDeadLetteringOnFilterEvaluationExceptions = true,
            EnableDeadLetteringOnMessageExpiration = true,
            ForwardDeadLetteredMessagesTo = "dead-letter-archive",
            ForwardTo = "orders-forward",
            LockDuration = TimeSpan.FromMinutes(2),
            MaxDeliveryCount = 17,
            RequiresSession = false,
            UserMetadata = "tenant=west",
        };

        CreateSubscriptionOptions options = configurator.GetCreateSubscriptionOptions();

        Assert.Equal("orders-topic", options.TopicName);
        Assert.Equal("orders-worker", options.SubscriptionName);
        Assert.Equal(TimeSpan.FromMinutes(17), options.AutoDeleteOnIdle);
        Assert.Equal(TimeSpan.FromDays(3), options.DefaultMessageTimeToLive);
        Assert.False(options.EnableBatchedOperations);
        Assert.True(options.EnableDeadLetteringOnFilterEvaluationExceptions);
        Assert.True(options.DeadLetteringOnMessageExpiration);
        Assert.Equal("dead-letter-archive", options.ForwardDeadLetteredMessagesTo);
        Assert.Equal("orders-forward", options.ForwardTo);
        Assert.Equal(TimeSpan.FromMinutes(2), options.LockDuration);
        Assert.Equal(17, options.MaxDeliveryCount);
        Assert.False(options.RequiresSession);
        Assert.Equal("tenant=west", options.UserMetadata);

        configurator.ForwardTo = null;
        configurator.RequiresSession = true;
        CreateSubscriptionOptions sessionOptions = configurator.GetCreateSubscriptionOptions();
        Assert.True(sessionOptions.RequiresSession);
        Assert.Null(sessionOptions.ForwardTo);
        Assert.Equal("orders-forward", options.ForwardTo);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-OPTIONS", "omitted-and-blank-settings-preserve-sdk-defaults")]
    public void OmittedAndBlankSettings_PreserveSdkDefaults()
    {
        var configurator = new ServiceBusSubscriptionConfigurator("orders-worker", "orders-topic")
        {
            ForwardDeadLetteredMessagesTo = " ",
            ForwardTo = "\t",
            UserMetadata = " ",
        };
        var sdkDefaults = new CreateSubscriptionOptions("orders-topic", "orders-worker");

        CreateSubscriptionOptions options = configurator.GetCreateSubscriptionOptions();

        Assert.Equal(sdkDefaults.AutoDeleteOnIdle, options.AutoDeleteOnIdle);
        Assert.Equal(TimeSpan.FromDays(366), options.DefaultMessageTimeToLive);
        Assert.Equal(sdkDefaults.EnableBatchedOperations, options.EnableBatchedOperations);
        Assert.Equal(sdkDefaults.EnableDeadLetteringOnFilterEvaluationExceptions,
            options.EnableDeadLetteringOnFilterEvaluationExceptions);
        Assert.Equal(sdkDefaults.DeadLetteringOnMessageExpiration, options.DeadLetteringOnMessageExpiration);
        Assert.Equal(sdkDefaults.ForwardDeadLetteredMessagesTo, options.ForwardDeadLetteredMessagesTo);
        Assert.Equal(sdkDefaults.ForwardTo, options.ForwardTo);
        Assert.Equal(sdkDefaults.LockDuration, options.LockDuration);
        Assert.Equal(sdkDefaults.MaxDeliveryCount, options.MaxDeliveryCount);
        Assert.Equal(sdkDefaults.RequiresSession, options.RequiresSession);
        Assert.Equal(sdkDefaults.UserMetadata, options.UserMetadata);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-OPTIONS", "explicit-zero-idle-uses-sdk-default-for-all-entity-kinds")]
    public void ExplicitZeroIdle_UsesSdkDefaultForSubscriptionsQueuesAndTopics()
    {
        var configurator = new ServiceBusSubscriptionConfigurator("orders-worker", "orders-topic")
        {
            AutoDeleteOnIdle = TimeSpan.Zero,
            DefaultMessageTimeToLive = null,
        };
        var sdkDefaults = new CreateSubscriptionOptions("orders-topic", "orders-worker");
        var queue = new ServiceBusQueueConfigurator("orders-queue") { AutoDeleteOnIdle = TimeSpan.Zero };
        var topic = new ServiceBusTopicConfigurator("orders-topic", false) { AutoDeleteOnIdle = TimeSpan.Zero };

        Assert.Empty(configurator.Validate());
        Assert.Empty(queue.Validate());
        Assert.Empty(topic.Validate());
        CreateSubscriptionOptions options = configurator.GetCreateSubscriptionOptions();
        CreateQueueOptions queueOptions = queue.GetCreateQueueOptions();
        CreateTopicOptions topicOptions = topic.GetCreateTopicOptions();

        Assert.Equal(sdkDefaults.AutoDeleteOnIdle, options.AutoDeleteOnIdle);
        Assert.Equal(sdkDefaults.DefaultMessageTimeToLive, options.DefaultMessageTimeToLive);
        Assert.Equal(new CreateQueueOptions("orders-queue").AutoDeleteOnIdle, queueOptions.AutoDeleteOnIdle);
        Assert.Equal(new CreateTopicOptions("orders-topic").AutoDeleteOnIdle, topicOptions.AutoDeleteOnIdle);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SUBSCRIPTION-OPTIONS", "zero-idle-addresses-and-send-settings-disable-deletion")]
    public void ZeroIdleAddressesAndSendSettings_DisableBrokerDeletion()
    {
        var host = new Uri("sb://localhost/test-scope");
        var queueAddress = new ServiceBusEndpointAddress(host, "orders-queue", TimeSpan.Zero);
        var topicAddress = new ServiceBusEndpointAddress(host, "orders-topic", TimeSpan.Zero,
            ServiceBusEndpointAddress.AddressType.Topic);
        var sendTopology = new ServiceBusSendTopology();

        var queueSettings = Assert.IsType<QueueSendSettings>(sendTopology.GetSendSettings(
            new ServiceBusEndpointAddress(host, (Uri)queueAddress)));
        var topicSettings = Assert.IsType<TopicSendSettings>(sendTopology.GetSendSettings(
            new ServiceBusEndpointAddress(host, (Uri)topicAddress)));
        TimeSpan queueIdle = Assert.Single(queueSettings.GetBrokerTopology().Queues).CreateQueueOptions.AutoDeleteOnIdle;
        TimeSpan topicIdle = Assert.Single(topicSettings.GetBrokerTopology().Topics).CreateTopicOptions.AutoDeleteOnIdle;

        var configuredSettings = new QueueSendSettings(new CreateQueueOptions("settings-queue")
        {
            AutoDeleteOnIdle = TimeSpan.FromMinutes(10),
        });
        configuredSettings.AutoDeleteOnIdle = TimeSpan.Zero;
        TimeSpan configuredIdle = Assert.Single(configuredSettings.GetBrokerTopology().Queues)
            .CreateQueueOptions.AutoDeleteOnIdle;

        Assert.Equal(TimeSpan.MaxValue, new CreateQueueOptions("orders-queue").AutoDeleteOnIdle);
        Assert.Equal(TimeSpan.MaxValue, new CreateTopicOptions("orders-topic").AutoDeleteOnIdle);
        Assert.Equal(TimeSpan.MaxValue, queueIdle);
        Assert.Equal(TimeSpan.MaxValue, topicIdle);
        Assert.Equal(TimeSpan.MaxValue, configuredIdle);
    }
}
