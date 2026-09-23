using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusQueueOptionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-QUEUE-OPTIONS", "configured-queue-settings-reach-sdk-options")]
    public void ConfiguredQueueSettings_ReachTheSdkOptionsWithoutLosingDistinctValues()
    {
        var configurator = new ServiceBusQueueConfigurator("orders-queue")
        {
            BasePath = "/region-west/",
            AutoDeleteOnIdle = TimeSpan.FromMinutes(17),
            DefaultMessageTimeToLive = TimeSpan.FromDays(3),
            DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(7),
            EnableBatchedOperations = false,
            EnableDeadLetteringOnMessageExpiration = false,
            EnablePartitioning = true,
            ForwardDeadLetteredMessagesTo = "dead-letter-archive",
            ForwardTo = "orders-forward",
            LockDuration = TimeSpan.FromMinutes(2),
            MaxDeliveryCount = 17,
            MaxSizeInMegabytes = 2048,
            RequiresDuplicateDetection = true,
            RequiresSession = false,
            UserMetadata = "tenant=west",
        };

        Assert.Empty(configurator.Validate());
        CreateQueueOptions options = configurator.GetCreateQueueOptions();

        Assert.Equal("region-west/orders-queue", options.Name);
        Assert.Equal(TimeSpan.FromMinutes(17), options.AutoDeleteOnIdle);
        Assert.Equal(TimeSpan.FromDays(3), options.DefaultMessageTimeToLive);
        Assert.Equal(TimeSpan.FromMinutes(7), options.DuplicateDetectionHistoryTimeWindow);
        Assert.False(options.EnableBatchedOperations);
        Assert.False(options.DeadLetteringOnMessageExpiration);
        Assert.True(options.EnablePartitioning);
        Assert.Equal("dead-letter-archive", options.ForwardDeadLetteredMessagesTo);
        Assert.Equal("orders-forward", options.ForwardTo);
        Assert.Equal(TimeSpan.FromMinutes(2), options.LockDuration);
        Assert.Equal(17, options.MaxDeliveryCount);
        Assert.Equal(2048, options.MaxSizeInMegabytes);
        Assert.Null(options.MaxMessageSizeInKilobytes);
        Assert.True(options.RequiresDuplicateDetection);
        Assert.False(options.RequiresSession);
        Assert.Equal("tenant=west", options.UserMetadata);

        configurator.ForwardTo = null;
        configurator.RequiresSession = true;
        configurator.EnablePartitioning = null;
        configurator.MaxMessageSizeInKilobytes = 1024;
        CreateQueueOptions premiumOptions = configurator.GetCreateQueueOptions();
        Assert.Null(premiumOptions.ForwardTo);
        Assert.True(premiumOptions.RequiresSession);
        Assert.False(premiumOptions.EnablePartitioning);
        Assert.Equal(1024, premiumOptions.MaxMessageSizeInKilobytes);
        Assert.Equal("orders-forward", options.ForwardTo);
        Assert.True(options.EnablePartitioning);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-QUEUE-OPTIONS", "unconfigured-optional-settings-and-blank-routes-preserve-defaults")]
    public void UnconfiguredOptionalSettingsAndBlankRoutes_PreserveProductAndSdkDefaults()
    {
        var configurator = new ServiceBusQueueConfigurator("orders-queue")
        {
            ForwardDeadLetteredMessagesTo = " ",
            ForwardTo = "\t",
            UserMetadata = " ",
        };
        var sdkDefaults = new CreateQueueOptions("orders-queue");

        CreateQueueOptions options = configurator.GetCreateQueueOptions();

        Assert.Equal(TimeSpan.FromDays(427), options.AutoDeleteOnIdle);
        Assert.Equal(TimeSpan.FromDays(366), options.DefaultMessageTimeToLive);
        Assert.True(options.EnableBatchedOperations);
        Assert.True(options.DeadLetteringOnMessageExpiration);
        Assert.Equal(TimeSpan.FromMinutes(5), options.LockDuration);
        Assert.Equal(5, options.MaxDeliveryCount);
        Assert.Equal(sdkDefaults.DuplicateDetectionHistoryTimeWindow, options.DuplicateDetectionHistoryTimeWindow);
        Assert.Equal(sdkDefaults.EnablePartitioning, options.EnablePartitioning);
        Assert.Equal(sdkDefaults.ForwardDeadLetteredMessagesTo, options.ForwardDeadLetteredMessagesTo);
        Assert.Equal(sdkDefaults.ForwardTo, options.ForwardTo);
        Assert.Equal(sdkDefaults.MaxSizeInMegabytes, options.MaxSizeInMegabytes);
        Assert.Equal(sdkDefaults.MaxMessageSizeInKilobytes, options.MaxMessageSizeInKilobytes);
        Assert.Equal(sdkDefaults.RequiresDuplicateDetection, options.RequiresDuplicateDetection);
        Assert.Equal(sdkDefaults.RequiresSession, options.RequiresSession);
        Assert.Equal(sdkDefaults.UserMetadata, options.UserMetadata);
    }
}
