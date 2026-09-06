using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>Provides queue declaration and processor settings for an Azure Service Bus receive endpoint.</summary>
public class ReceiveEndpointSettings :
    BaseClientSettings,
    ReceiveSettings
{
    readonly ServiceBusQueueConfigurator _queueConfigurator;

    /// <summary>Creates endpoint settings from a queue configuration.</summary>
    /// <param name="endpointConfiguration">The endpoint configuration supplying transport settings.</param>
    /// <param name="queueName">The processor queue name.</param>
    /// <param name="queueConfigurator">The Azure queue configuration.</param>
    public ReceiveEndpointSettings(IServiceBusEndpointConfiguration endpointConfiguration, string queueName, ServiceBusQueueConfigurator queueConfigurator)
        : base(endpointConfiguration, queueConfigurator)
    {
        _queueConfigurator = queueConfigurator;

        Name = queueName;
    }

    /// <summary>Gets the Azure queue configuration.</summary>
    public IServiceBusQueueConfigurator QueueConfigurator => _queueConfigurator;

    /// <summary>Gets whether the queue requires sessions.</summary>
    public override bool RequiresSession => _queueConfigurator.RequiresSession ?? false;

    /// <summary>Gets or sets whether temporary subscriptions created for the endpoint are deleted during shutdown.</summary>
    public bool RemoveSubscriptions { get; set; }
    /// <summary>Gets the maximum number of sessions processed concurrently.</summary>
    public override int MaxConcurrentSessions => _queueConfigurator.MaxConcurrentSessions ?? MaxConcurrentCalls;
    /// <summary>Gets the maximum number of concurrent message callbacks per session.</summary>
    public override int MaxConcurrentCallsPerSession => _queueConfigurator.MaxConcurrentCallsPerSession ?? Defaults.MaxConcurrentCallsPerSessions;

    /// <summary>Gets the namespace-relative queue path.</summary>
    public override string Path => _queueConfigurator.FullPath;

    /// <summary>Builds the Azure queue declaration options.</summary>
    /// <returns>The queue declaration options.</returns>
    public CreateQueueOptions GetCreateQueueOptions()
    {
        return _queueConfigurator.GetCreateQueueOptions();
    }

    /// <summary>Formats non-default queue options for the endpoint input address.</summary>
    /// <returns>The encoded option fragments.</returns>
    protected override IEnumerable<string> GetQueryStringOptions()
    {
        if (_queueConfigurator.AutoDeleteOnIdle.HasValue && _queueConfigurator.AutoDeleteOnIdle.Value > TimeSpan.Zero
            && _queueConfigurator.AutoDeleteOnIdle.Value != Defaults.AutoDeleteOnIdle)
            yield return $"autodelete={_queueConfigurator.AutoDeleteOnIdle.Value.TotalSeconds}";
    }
}
