using System;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq bus topology implementation.
/// </summary>
public class RabbitMqBusTopology :
    BusTopology,
    IRabbitMqBusTopology
{
    readonly IRabbitMqTopologyConfiguration _configuration;
    readonly IRabbitMqHostConfiguration _hostConfiguration;
    readonly IMessageNameFormatter _messageNameFormatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="messageNameFormatter">The message name formatter value.</param>
    /// <param name="configuration">The configuration callback.</param>
    public RabbitMqBusTopology(IRabbitMqHostConfiguration hostConfiguration, IMessageNameFormatter messageNameFormatter,
        IRabbitMqTopologyConfiguration configuration)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _messageNameFormatter = messageNameFormatter;
        _configuration = configuration;
    }

    IRabbitMqPublishTopology IRabbitMqBusTopology.PublishTopology => _configuration.Publish;
    IRabbitMqSendTopology IRabbitMqBusTopology.SendTopology => _configuration.Send;

    IRabbitMqMessagePublishTopology<T> IRabbitMqBusTopology.Publish<T>()
    {
        return _configuration.Publish.GetMessageTopology<T>();
    }

    IRabbitMqMessageSendTopology<T> IRabbitMqBusTopology.Send<T>()
    {
        return _configuration.Send.GetMessageTopology<T>();
    }

    /// <summary>
    /// Gets destination address.
    /// </summary>
    /// <param name="exchangeName">The exchange name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public Uri GetDestinationAddress(string exchangeName, Action<IRabbitMqExchangeConfigurator>? configure = null)
    {
        var hostAddress = _hostConfiguration.HostAddress;
        var address = new RabbitMqEndpointAddress(hostAddress, exchangeName);

        var sendSettings = new RabbitMqSendSettings(address);

        configure?.Invoke(sendSettings);

        return sendSettings.GetSendAddress(hostAddress);
    }

    /// <summary>
    /// Gets destination address.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public Uri GetDestinationAddress(Type messageType, Action<IRabbitMqExchangeConfigurator>? configure = null)
    {
        var hostAddress = _hostConfiguration.HostAddress;
        var exchangeName = _messageNameFormatter.GetMessageName(messageType).ToString();
        var isTemporary = MessageTypeCache.IsTemporaryMessageType(messageType);
        var address = new RabbitMqEndpointAddress(
            hostAddress,
            exchangeName,
            durable: !isTemporary,
            autoDelete: isTemporary);

        var settings = new RabbitMqSendSettings(address);

        configure?.Invoke(settings);

        return settings.GetSendAddress(hostAddress);
    }
}
