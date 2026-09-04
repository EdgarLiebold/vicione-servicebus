using System;
using System.Collections.Generic;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Topology;

/// <summary>
/// Provides a receive endpoint settings implementation.
/// </summary>
public class ReceiveEndpointSettings :
    BaseClientSettings,
    ReceiveSettings
{
    readonly ServiceBusQueueConfigurator _queueConfigurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpointConfiguration">The endpoint configuration value.</param>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="queueConfigurator">The queue configurator value.</param>
    public ReceiveEndpointSettings(IServiceBusEndpointConfiguration endpointConfiguration, string queueName, ServiceBusQueueConfigurator queueConfigurator)
        : base(endpointConfiguration, queueConfigurator)
    {
        _queueConfigurator = queueConfigurator;

        Name = queueName;
    }

    /// <summary>
    /// Gets the queue configurator value.
    /// </summary>
    public IServiceBusQueueConfigurator QueueConfigurator => _queueConfigurator;

    /// <summary>
    /// Gets the requires session value.
    /// </summary>
    public override bool RequiresSession => _queueConfigurator.RequiresSession ?? false;

    /// <summary>
    /// Gets or sets the remove subscriptions value.
    /// </summary>
    public bool RemoveSubscriptions { get; set; }
    /// <summary>
    /// Gets the max concurrent sessions value.
    /// </summary>
    public override int MaxConcurrentSessions => _queueConfigurator.MaxConcurrentSessions ?? MaxConcurrentCalls;
    /// <summary>
    /// Gets the max concurrent calls per session value.
    /// </summary>
    public override int MaxConcurrentCallsPerSession => _queueConfigurator.MaxConcurrentCallsPerSession ?? Defaults.MaxConcurrentCallsPerSessions;

    /// <summary>
    /// Gets the path value.
    /// </summary>
    public override string Path => _queueConfigurator.FullPath;

    /// <summary>
    /// Gets create queue options.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public CreateQueueOptions GetCreateQueueOptions()
    {
        return _queueConfigurator.GetCreateQueueOptions();
    }

    /// <summary>
    /// Gets query string options.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    protected override IEnumerable<string> GetQueryStringOptions()
    {
        if (_queueConfigurator.AutoDeleteOnIdle.HasValue && _queueConfigurator.AutoDeleteOnIdle.Value > TimeSpan.Zero
            && _queueConfigurator.AutoDeleteOnIdle.Value != Defaults.AutoDeleteOnIdle)
            yield return $"autodelete={_queueConfigurator.AutoDeleteOnIdle.Value.TotalSeconds}";
    }
}
