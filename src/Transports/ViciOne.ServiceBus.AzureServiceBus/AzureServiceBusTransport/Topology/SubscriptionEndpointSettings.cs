using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a subscription endpoint settings implementation.
/// </summary>
public class SubscriptionEndpointSettings :
    BaseClientSettings,
    SubscriptionSettings
{
    readonly CreateTopicOptions _createTopicOptions;
    readonly ServiceBusSubscriptionConfigurator _subscriptionConfigurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="topicName">The topic name value.</param>
    public SubscriptionEndpointSettings(IServiceBusEndpointConfiguration configuration, string subscriptionName, string topicName)
        : this(configuration, subscriptionName, Defaults.GetCreateTopicOptions(topicName))
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="subscriptionName">The subscription name value.</param>
    /// <param name="createTopicOptions">The create topic options value.</param>
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

    /// <summary>
    /// Gets the subscription configurator value.
    /// </summary>
    public IServiceBusSubscriptionConfigurator SubscriptionConfigurator => _subscriptionConfigurator;

    /// <summary>
    /// Gets the requires session value.
    /// </summary>
    public override bool RequiresSession => _subscriptionConfigurator.RequiresSession ?? false;

    /// <summary>
    /// Gets or sets the remove subscriptions value.
    /// </summary>
    public bool RemoveSubscriptions { get; set; }
    /// <summary>
    /// Gets the max concurrent sessions value.
    /// </summary>
    public override int MaxConcurrentSessions => _subscriptionConfigurator.MaxConcurrentSessions ?? MaxConcurrentCalls;
    /// <summary>
    /// Gets the max concurrent calls per session value.
    /// </summary>
    public override int MaxConcurrentCallsPerSession => _subscriptionConfigurator.MaxConcurrentCallsPerSession ?? Defaults.MaxConcurrentCallsPerSessions;

    CreateTopicOptions SubscriptionSettings.CreateTopicOptions => _createTopicOptions;
    CreateSubscriptionOptions SubscriptionSettings.CreateSubscriptionOptions => _subscriptionConfigurator.GetCreateSubscriptionOptions();

    /// <summary>
    /// Gets or sets the rule value.
    /// </summary>
    public CreateRuleOptions? Rule { get; set; }
    /// <summary>
    /// Gets or sets the filter value.
    /// </summary>
    public RuleFilter? Filter { get; set; }

    /// <summary>
    /// Gets the path value.
    /// </summary>
    public override string Path { get; }

    /// <summary>
    /// Gets query string options.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IEnumerable<string> GetQueryStringOptions()
    {
        if (_subscriptionConfigurator.AutoDeleteOnIdle.HasValue && _subscriptionConfigurator.AutoDeleteOnIdle.Value > TimeSpan.Zero
            && _subscriptionConfigurator.AutoDeleteOnIdle.Value != Defaults.AutoDeleteOnIdle)
            yield return $"autodelete={_subscriptionConfigurator.AutoDeleteOnIdle.Value.TotalSeconds}";
    }
}
