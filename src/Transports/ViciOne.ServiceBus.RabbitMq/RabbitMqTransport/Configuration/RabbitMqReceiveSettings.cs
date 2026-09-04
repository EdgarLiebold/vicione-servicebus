using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace ViciOne.ServiceBus.RabbitMq.Configuration;

/// <summary>
/// Provides a rabbit mq receive settings implementation.
/// </summary>
public class RabbitMqReceiveSettings :
    QueueBindingConfigurator,
    ReceiveSettings
{
    readonly IRabbitMqEndpointConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="configuration">The configuration callback.</param>
    /// <param name="name">The name value.</param>
    /// <param name="type">The type value.</param>
    /// <param name="durable">The durable value.</param>
    /// <param name="autoDelete">The auto delete value.</param>
    public RabbitMqReceiveSettings(IRabbitMqEndpointConfiguration configuration, string name, string type, bool durable, bool autoDelete)
        : base(name, type, durable, autoDelete)
    {
        _configuration = configuration;

        ConsumeArguments = new Dictionary<string, object?>();
    }

    /// <summary>
    /// Gets or sets the consumer priority value.
    /// </summary>
    public int ConsumerPriority
    {
        set => ConsumeArguments[RabbitMQ.Client.Headers.XPriority] = value;
    }

    /// <summary>
    /// Gets or sets the prefetch count value.
    /// </summary>
    public ushort PrefetchCount
    {
        get => (ushort)_configuration.Transport.PrefetchCount;
        set => _configuration.Transport.Configurator.PrefetchCount = value;
    }

    /// <summary>
    /// Gets or sets the purge on startup value.
    /// </summary>
    public bool PurgeOnStartup { get; set; }
    /// <summary>
    /// Gets or sets the exclusive consumer value.
    /// </summary>
    public bool ExclusiveConsumer { get; set; }
    /// <summary>
    /// Gets or sets the no ack value.
    /// </summary>
    public bool NoAck { get; set; }

    /// <summary>
    /// Gets or sets the bind queue value.
    /// </summary>
    public bool BindQueue { get; set; } = true;

    /// <summary>
    /// Gets the consume arguments value.
    /// </summary>
    public IDictionary<string, object?> ConsumeArguments { get; }

    /// <summary>
    /// Gets or sets the consumer tag value.
    /// </summary>
    public string ConsumerTag { get; set; } = "";

    /// <summary>
    /// Gets input address.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    public Uri GetInputAddress(Uri hostAddress)
    {
        return GetEndpointAddress(hostAddress);
    }
}
