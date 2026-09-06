using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Provides topic, subscription, and processor settings for an Azure Service Bus endpoint.</summary>
public class SubscriptionEndpointSettings :
    BaseClientSettings,
    SubscriptionSettings
{
    readonly CreateTopicOptions _createTopicOptions;
    readonly ServiceBusSubscriptionConfigurator _subscriptionConfigurator;

    /// <summary>Creates endpoint settings for a named topic and subscription.</summary>
    /// <param name="configuration">The endpoint configuration supplying transport settings.</param>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="topicName">The namespace-relative topic name.</param>
    public SubscriptionEndpointSettings(IServiceBusEndpointConfiguration configuration, string subscriptionName, string topicName)
        : this(configuration, subscriptionName, Defaults.GetCreateTopicOptions(topicName))
    {
    }

    /// <summary>Creates endpoint settings from topic declaration options.</summary>
    /// <param name="configuration">The endpoint configuration supplying transport settings.</param>
    /// <param name="subscriptionName">The subscription name.</param>
    /// <param name="createTopicOptions">The Azure topic declaration options.</param>
    public SubscriptionEndpointSettings(IServiceBusEndpointConfiguration configuration, string subscriptionName, CreateTopicOptions createTopicOptions)
        : this(configuration, createTopicOptions, new ServiceBusSubscriptionConfigurator(subscriptionName, createTopicOptions.Name))
    {
    }

    SubscriptionEndpointSettings(IServiceBusEndpointConfiguration configuration, CreateTopicOptions createTopicOptions,
        ServiceBusSubscriptionConfigurator configurator)
        : base(configuration, configurator)
    {
        _createTopicOptions = createTopicOptions;
        _subscriptionConfigurator = configurator;

        Name = Path = EntityNameFormatter.FormatSubscriptionPath(_subscriptionConfigurator.TopicPath, _subscriptionConfigurator.SubscriptionName);
    }

    /// <summary>Gets the Azure subscription configuration.</summary>
    public IServiceBusSubscriptionConfigurator SubscriptionConfigurator => _subscriptionConfigurator;

    /// <summary>Gets whether the subscription requires sessions.</summary>
    public override bool RequiresSession => _subscriptionConfigurator.RequiresSession ?? false;

    /// <summary>Gets or sets whether the subscription is deleted during endpoint shutdown.</summary>
    public bool RemoveSubscriptions { get; set; }
    /// <summary>Gets the maximum number of sessions processed concurrently.</summary>
    public override int MaxConcurrentSessions => _subscriptionConfigurator.MaxConcurrentSessions ?? MaxConcurrentCalls;
    /// <summary>Gets the maximum number of concurrent message callbacks per session.</summary>
    public override int MaxConcurrentCallsPerSession => _subscriptionConfigurator.MaxConcurrentCallsPerSession ?? Defaults.MaxConcurrentCallsPerSessions;

    CreateTopicOptions SubscriptionSettings.CreateTopicOptions => _createTopicOptions;
    CreateSubscriptionOptions SubscriptionSettings.CreateSubscriptionOptions => _subscriptionConfigurator.GetCreateSubscriptionOptions();

    /// <summary>Gets or sets the initial Azure subscription rule.</summary>
    public CreateRuleOptions? Rule { get; set; }
    /// <summary>Gets or sets the filter associated with the initial subscription rule.</summary>
    public RuleFilter? Filter { get; set; }

    /// <summary>Gets the combined topic and subscription path.</summary>
    public override string Path { get; }

    /// <summary>Formats non-default subscription options for the endpoint input address.</summary>
    /// <returns>The encoded option fragments.</returns>
    protected override IEnumerable<string> GetQueryStringOptions()
    {
        if (_subscriptionConfigurator.AutoDeleteOnIdle.HasValue && _subscriptionConfigurator.AutoDeleteOnIdle.Value > TimeSpan.Zero
            && _subscriptionConfigurator.AutoDeleteOnIdle.Value != Defaults.AutoDeleteOnIdle)
            yield return $"autodelete={_subscriptionConfigurator.AutoDeleteOnIdle.Value.TotalSeconds}";
    }
}
