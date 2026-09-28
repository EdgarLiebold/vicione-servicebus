using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>Stores RabbitMQ receive, queue, exchange, binding, and consumer settings.</summary>
public class RabbitMqReceiveSettings :
    QueueBindingConfigurator,
    ReceiveSettings
{
    readonly IRabbitMqEndpointConfiguration _configuration;

    /// <summary>Creates matching receive queue and exchange settings.</summary>
    /// <param name="configuration">The endpoint configuration that owns transport concurrency settings.</param>
    /// <param name="name">The queue and exchange name.</param>
    /// <param name="type">The RabbitMQ exchange type.</param>
    /// <param name="durable">Whether endpoint topology survives broker restarts.</param>
    /// <param name="autoDelete">Whether RabbitMQ deletes endpoint topology when unused.</param>
    public RabbitMqReceiveSettings(IRabbitMqEndpointConfiguration configuration, string name, string type, bool durable, bool autoDelete)
        : base(name, type, durable, autoDelete)
    {
        _configuration = configuration;

        ConsumeArguments = new Dictionary<string, object?>();
    }

    /// <summary>Sets the RabbitMQ consumer priority argument.</summary>
    public int ConsumerPriority
    {
        set => ConsumeArguments[RabbitMQ.Client.Headers.XPriority] = value;
    }

    /// <summary>Gets or sets the prefetch count.</summary>
    public ushort PrefetchCount
    {
        get
        {
            int value = _configuration.Transport.PrefetchCount;
            if (value < 0 || value > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(PrefetchCount), value, "RabbitMQ prefetch count must be between 0 and 65535.");

            return (ushort)value;
        }
        set => _configuration.Transport.Configurator.PrefetchCount = value;
    }

    /// <summary>Gets or sets the purge on startup.</summary>
    public bool PurgeOnStartup { get; set; }
    /// <summary>Gets or sets the exclusive consumer.</summary>
    public bool ExclusiveConsumer { get; set; }
    /// <summary>Gets or sets whether RabbitMQ considers deliveries acknowledged immediately.</summary>
    public bool NoAck { get; set; }

    /// <summary>Gets or sets whether deployment includes the queue and its exchange binding.</summary>
    public bool BindQueue { get; set; } = true;

    /// <summary>Gets the consume arguments.</summary>
    public IDictionary<string, object?> ConsumeArguments { get; }

    /// <summary>Gets or sets the consumer tag.</summary>
    public string ConsumerTag { get; set; } = "";

    /// <summary>Creates the normalized input address from the receive topology settings.</summary>
    /// <param name="hostAddress">The RabbitMQ host and virtual-host address.</param>
    /// <returns>The receive endpoint address.</returns>
    public Uri GetInputAddress(Uri hostAddress)
    {
        return GetEndpointAddress(hostAddress);
    }
}
