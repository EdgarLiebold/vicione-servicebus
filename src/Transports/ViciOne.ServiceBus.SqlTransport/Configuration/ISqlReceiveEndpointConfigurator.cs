using System;
using ViciOne.ServiceBus.SqlTransport;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configure a database transport receive endpoint.</summary>
public interface ISqlReceiveEndpointConfigurator :
    IReceiveEndpointConfigurator,
    IPartitionedReceiveEndpointConfigurator,
    ISqlQueueEndpointConfigurator
{
    /// <summary>
    /// The time to wait before the message is redelivered when faults are rethrown to the transport.
    /// Defaults to 0.
    /// </summary>
    TimeSpan? UnlockDelay { set; }

    /// <summary>
    /// Set number of concurrent messages per PartitionKey, higher value will increase throughput but will break delivery order (default: 1).
    /// This applies to the concurrent receive modes only.
    /// </summary>
    int ConcurrentDeliveryLimit { set; }

    /// <summary>Set the endpoint receive mode (changes the delivery behavior of messages to use partition keys, ordering, etc.</summary>
    /// <param name="mode">The mode.</param>
    /// <param name="concurrentDeliveryLimit">The concurrent delivery limit.</param>
    void SetReceiveMode(SqlReceiveMode mode, int? concurrentDeliveryLimit = default);

    /// <summary>Adds a topic subscription to the receive endpoint by message type.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="callback">The callback invoked by the operation.</param>
    void Subscribe<T>(Action<ISqlTopicSubscriptionConfigurator>? callback = null)
        where T : class;

    /// <summary>Adds a topic subscription to the receive endpoint.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="callback">Configure the topic and the subscription.</param>
    void Subscribe(string topicName, Action<ISqlTopicSubscriptionConfigurator>? callback = default);

    /// <summary>Add middleware to the receive endpoint <see cref="ClientContext" /> pipe.</summary>
    /// <param name="configure">The callback used to configure the component.</param>
    void ConfigureClient(Action<IPipeConfigurator<ClientContext>>? configure);
}
