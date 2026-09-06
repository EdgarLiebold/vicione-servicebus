using System;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>Configures sql queue.</summary>
public class SqlQueueConfigurator :
    ISqlQueueConfigurator,
    Queue
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="queueName">The queue name.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle.</param>
    protected SqlQueueConfigurator(string queueName, TimeSpan? autoDeleteOnIdle = null)
    {
        QueueName = queueName;
        AutoDeleteOnIdle = autoDeleteOnIdle;
    }

    /// <summary>Gets or sets the auto delete on idle.</summary>
    public TimeSpan? AutoDeleteOnIdle { get; set; }

    /// <summary>Gets or sets the max delivery count.</summary>
    public int? MaxDeliveryCount { get; set; }

    /// <summary>Gets or sets the queue name.</summary>
    public string QueueName { get; set; }

    /// <summary>Gets endpoint address.</summary>
    /// <param name="hostAddress">The host address.</param>
    /// <returns>The endpoint address.</returns>
    protected SqlEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new SqlEndpointAddress(hostAddress, QueueName, AutoDeleteOnIdle);
    }
}
