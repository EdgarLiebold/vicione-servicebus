using System;
using ViciOne.ServiceBus.RabbitMq.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Provides RabbitMQ send and publish topology and constructs provider-specific destination addresses.</summary>
public class RabbitMqBusTopology :
    BusTopology,
    IRabbitMqBusTopology
{
    readonly IRabbitMqTopologyConfiguration _configuration;
    readonly IRabbitMqHostConfiguration _hostConfiguration;
    readonly IMessageNameFormatter _messageNameFormatter;

    /// <summary>Creates the bus topology for a RabbitMQ host.</summary>
    /// <param name="hostConfiguration">The host that supplies the base destination address.</param>
    /// <param name="messageNameFormatter">The formatter used to derive exchange names from message contracts.</param>
    /// <param name="configuration">The RabbitMQ send and publish topology configuration.</param>
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

    /// <summary>Builds a destination address for a named exchange.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="configure">An optional callback that customizes the exchange settings encoded in the address.</param>
    /// <returns>The RabbitMQ destination address.</returns>
    public Uri GetDestinationAddress(string exchangeName, Action<IRabbitMqExchangeConfigurator>? configure = null)
    {
        var hostAddress = _hostConfiguration.HostAddress;
        var address = new RabbitMqEndpointAddress(hostAddress, exchangeName);

        var sendSettings = new RabbitMqSendSettings(address);

        configure?.Invoke(sendSettings);

        return sendSettings.GetSendAddress(hostAddress);
    }

    /// <summary>Builds a destination address for a message contract's exchange.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that customizes the exchange settings encoded in the address.</param>
    /// <returns>The RabbitMQ destination address; temporary message contracts use non-durable, auto-delete exchanges.</returns>
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
