using System;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.SqlTransport.Configuration;

/// <summary>
/// Provides a sql queue configurator implementation.
/// </summary>
public class SqlQueueConfigurator :
    ISqlQueueConfigurator,
    Queue
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="queueName">The queue name value.</param>
    /// <param name="autoDeleteOnIdle">The auto delete on idle value.</param>
    protected SqlQueueConfigurator(string queueName, TimeSpan? autoDeleteOnIdle = null)
    {
        QueueName = queueName;
        AutoDeleteOnIdle = autoDeleteOnIdle;
    }

    /// <summary>
    /// Gets or sets the auto delete on idle value.
    /// </summary>
    public TimeSpan? AutoDeleteOnIdle { get; set; }

    /// <summary>
    /// Gets or sets the max delivery count value.
    /// </summary>
    public int? MaxDeliveryCount { get; set; }

    /// <summary>
    /// Gets or sets the queue name value.
    /// </summary>
    public string QueueName { get; set; }

    /// <summary>
    /// Gets endpoint address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    protected SqlEndpointAddress GetEndpointAddress(Uri hostAddress)
    {
        return new SqlEndpointAddress(hostAddress, QueueName, AutoDeleteOnIdle);
    }
}
