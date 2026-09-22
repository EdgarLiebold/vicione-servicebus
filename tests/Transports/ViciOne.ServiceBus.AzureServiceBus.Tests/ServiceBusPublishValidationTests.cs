using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusPublishValidationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PUBLISH-VALIDATION", "invalid-topic-name-and-idle-duration-reach-publish-validation")]
    public void InvalidTopicNameAndIdleDuration_AreReportedBeforeBrokerTopologyIsBuilt()
    {
        IMessageTopologyConfigurator messages = AzureBusFactory.CreateMessageTopology();
        messages.GetMessageTopology<InvalidPathMessage>().SetEntityName("invalid topic name");
        messages.GetMessageTopology<InvalidIdleMessage>().SetEntityName("valid-topic");
        var publish = new ServiceBusPublishTopology(messages);
        var configurator = (IServiceBusPublishTopologyConfigurator)publish;
        _ = configurator.GetMessageTopology<InvalidPathMessage>();
        configurator.GetMessageTopology<InvalidIdleMessage>().AutoDeleteOnIdle = TimeSpan.FromMinutes(4);

        ValidationResult[] failures = publish.Validate().ToArray();

        Assert.Equal(2, failures.Length);
        Assert.Contains(failures, failure => failure.Key == "Path"
            && failure.Disposition == ValidationResultDisposition.Failure
            && failure.Message.Contains("invalid topic name", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Key == "AutoDeleteOnIdle"
            && failure.Disposition == ValidationResultDisposition.Failure
            && failure.Message.Contains(">= 5:00", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PUBLISH-VALIDATION", "five-minute-idle-boundary-is-accepted")]
    public void FiveMinuteIdleDurationAndValidTopicName_PassPublishValidation()
    {
        IMessageTopologyConfigurator messages = AzureBusFactory.CreateMessageTopology();
        messages.GetMessageTopology<BoundaryMessage>().SetEntityName("valid-topic");
        var publish = new ServiceBusPublishTopology(messages);
        var configurator = (IServiceBusPublishTopologyConfigurator)publish;
        configurator.GetMessageTopology<BoundaryMessage>().AutoDeleteOnIdle = TimeSpan.FromMinutes(5);

        Assert.Empty(publish.Validate());
        var declared = Assert.Single(publish.GetPublishBrokerTopology().Topics).CreateTopicOptions;
        Assert.Equal("valid-topic", declared.Name);
        Assert.Equal(TimeSpan.FromMinutes(5), declared.AutoDeleteOnIdle);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PUBLISH-VALIDATION", "invalid-composed-topic-path-is-rejected")]
    public void BasePath_RejectsInvalidCharactersAndCombinedLength()
    {
        IMessageTopologyConfigurator messages = AzureBusFactory.CreateMessageTopology();
        messages.GetMessageTopology<InvalidPrefixMessage>().SetEntityName("child");
        messages.GetMessageTopology<OverlongPrefixMessage>().SetEntityName("child");
        var publish = new ServiceBusPublishTopology(messages);
        var configurator = (IServiceBusPublishTopologyConfigurator)publish;
        configurator.GetMessageTopology<InvalidPrefixMessage>().BasePath = "invalid prefix";
        configurator.GetMessageTopology<OverlongPrefixMessage>().BasePath = new string('a', 256);

        ValidationResult[] failures = publish.Validate().ToArray();

        Assert.Equal(2, failures.Length);
        Assert.All(failures, failure => Assert.Equal("Path", failure.Key));
        Assert.Contains(failures, failure => failure.Message.Contains("invalid prefix/child", StringComparison.Ordinal));
        Assert.Contains(failures, failure => failure.Message.Contains(new string('a', 256) + "/child", StringComparison.Ordinal));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PUBLISH-VALIDATION", "excluded-topic-does-not-block-publish-validation")]
    public void ExcludedTopic_DoesNotFailValidationOrCreateABrokerDeclaration()
    {
        IMessageTopologyConfigurator messages = AzureBusFactory.CreateMessageTopology();
        messages.GetMessageTopology<ExcludedMessage>().SetEntityName("invalid topic name");
        var publish = new ServiceBusPublishTopology(messages);
        var configurator = (IServiceBusPublishTopologyConfigurator)publish;
        var excluded = configurator.GetMessageTopology<ExcludedMessage>();
        excluded.AutoDeleteOnIdle = TimeSpan.FromMinutes(4);
        excluded.Exclude = true;

        Assert.Empty(publish.Validate());
        Assert.Empty(publish.GetPublishBrokerTopology().Topics);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PUBLISH-VALIDATION", "subscribe-freezes-topic-options-before-later-changes")]
    public void SubscriptionSnapshot_RejectsLaterPublishChangesAndKeepsSenderAligned()
    {
        IMessageTopologyConfigurator messages = AzureBusFactory.CreateMessageTopology();
        messages.GetMessageTopology<SnapshotMessage>().SetEntityName("child");
        var publish = new ServiceBusPublishTopology(messages);
        var consume = new ServiceBusConsumeTopology(messages, publish);
        ((IServiceBusConsumeTopologyConfigurator)consume).GetMessageTopology<SnapshotMessage>().Subscribe("consumer");
        var topic = ((IServiceBusPublishTopologyConfigurator)publish).GetMessageTopology<SnapshotMessage>();

        InvalidOperationException pathFailure = Assert.Throws<InvalidOperationException>(() => topic.BasePath = "prefix");
        InvalidOperationException ttlFailure = Assert.Throws<InvalidOperationException>(() =>
            topic.DefaultMessageTimeToLive = TimeSpan.FromHours(1));

        Assert.Contains("already evaluated", pathFailure.Message, StringComparison.Ordinal);
        Assert.Contains("already evaluated", ttlFailure.Message, StringComparison.Ordinal);
        Assert.Equal("child", topic.CreateTopicOptions.Name);
        Assert.Equal("child", Assert.Single(publish.GetPublishBrokerTopology().Topics).CreateTopicOptions.Name);
        Assert.Equal("child", Assert.IsType<TopicSendSettings>(topic.GetSendSettings()).EntityPath);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PUBLISH-VALIDATION", "exposed-topic-options-cannot-mutate-broker-declaration")]
    public void ExposedTopicOptions_CannotChangeTheValidatedBrokerDeclaration()
    {
        IMessageTopologyConfigurator messages = AzureBusFactory.CreateMessageTopology();
        messages.GetMessageTopology<MutableOptionsMessage>().SetEntityName("child");
        var publish = new ServiceBusPublishTopology(messages);
        var topic = ((IServiceBusPublishTopologyConfigurator)publish).GetMessageTopology<MutableOptionsMessage>();
        var exposed = topic.CreateTopicOptions;
        TimeSpan expectedIdle = exposed.AutoDeleteOnIdle;
        exposed.AutoDeleteOnIdle = TimeSpan.FromMinutes(7);
        exposed.Status = EntityStatus.Disabled;
        exposed.AuthorizationRules.Add(new SharedAccessAuthorizationRule("external", [AccessRights.Listen]));

        Assert.Empty(publish.Validate());
        var projected = topic.CreateTopicOptions;
        var declared = Assert.Single(publish.GetPublishBrokerTopology().Topics).CreateTopicOptions;
        Assert.Equal(expectedIdle, projected.AutoDeleteOnIdle);
        Assert.Equal(expectedIdle, declared.AutoDeleteOnIdle);
        Assert.Equal(EntityStatus.Active, projected.Status);
        Assert.Equal(EntityStatus.Active, declared.Status);
        Assert.Empty(projected.AuthorizationRules);
        Assert.Empty(declared.AuthorizationRules);
        Assert.Equal("child", Assert.IsType<TopicSendSettings>(topic.GetSendSettings()).EntityPath);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PUBLISH-VALIDATION", "configured-topic-properties-reach-broker-and-sender")]
    public void ConfiguredTopicProperties_ReachTheBrokerAndSenderWithoutLoss()
    {
        IMessageTopologyConfigurator messages = AzureBusFactory.CreateMessageTopology();
        messages.GetMessageTopology<ConfiguredOptionsMessage>().SetEntityName("child");
        var publish = new ServiceBusPublishTopology(messages);
        var topic = ((IServiceBusPublishTopologyConfigurator)publish).GetMessageTopology<ConfiguredOptionsMessage>();
        topic.BasePath = "prefix";
        topic.AutoDeleteOnIdle = TimeSpan.FromMinutes(9);
        topic.DefaultMessageTimeToLive = TimeSpan.FromDays(2);
        topic.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(10);
        topic.EnableBatchedOperations = false;
        topic.EnablePartitioning = true;
        topic.MaxSizeInMegabytes = 2048;
        topic.MaxMessageSizeInKilobytes = 1024;
        topic.RequiresDuplicateDetection = true;
        topic.SupportOrdering = true;
        topic.UserMetadata = "tenant=alpha";

        Assert.Empty(publish.Validate());
        var declared = Assert.Single(publish.GetPublishBrokerTopology().Topics).CreateTopicOptions;
        var sender = Assert.IsType<TopicSendSettings>(topic.GetSendSettings());
        Assert.Equal("prefix/child", sender.EntityPath);
        AssertConfiguredOptions(declared);
        AssertConfiguredOptions(Assert.Single(sender.GetBrokerTopology().Topics).CreateTopicOptions);

        static void AssertConfiguredOptions(CreateTopicOptions options)
        {
            Assert.Equal("prefix/child", options.Name);
            Assert.Equal(TimeSpan.FromMinutes(9), options.AutoDeleteOnIdle);
            Assert.Equal(TimeSpan.FromDays(2), options.DefaultMessageTimeToLive);
            Assert.Equal(TimeSpan.FromMinutes(10), options.DuplicateDetectionHistoryTimeWindow);
            Assert.False(options.EnableBatchedOperations);
            Assert.True(options.EnablePartitioning);
            Assert.Equal(2048, options.MaxSizeInMegabytes);
            Assert.Equal(1024, options.MaxMessageSizeInKilobytes);
            Assert.True(options.RequiresDuplicateDetection);
            Assert.True(options.SupportOrdering);
            Assert.Equal("tenant=alpha", options.UserMetadata);
        }
    }

    public sealed record InvalidPathMessage;
    public sealed record InvalidIdleMessage;
    public sealed record BoundaryMessage;
    public sealed record InvalidPrefixMessage;
    public sealed record OverlongPrefixMessage;
    public sealed record ExcludedMessage;
    public sealed record SnapshotMessage;
    public sealed record MutableOptionsMessage;
    public sealed record ConfiguredOptionsMessage;
}
