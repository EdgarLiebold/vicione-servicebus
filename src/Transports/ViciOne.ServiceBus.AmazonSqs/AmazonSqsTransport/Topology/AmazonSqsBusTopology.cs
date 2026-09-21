using System;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>Resolves Amazon SQS send and Amazon SNS publish topology and destination addresses for a bus.</summary>
public class AmazonSqsBusTopology :
    BusTopology,
    IAmazonSqsBusTopology
{
    readonly IAmazonSqsTopologyConfiguration _configuration;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly IMessageNameFormatter _messageNameFormatter;

    /// <summary>Initializes Amazon SQS bus topology.</summary>
    /// <param name="hostConfiguration">The host used to resolve relative entity addresses.</param>
    /// <param name="messageNameFormatter">The formatter used to derive topic names from message types.</param>
    /// <param name="configuration">The send and publish topology configuration.</param>
    public AmazonSqsBusTopology(IAmazonSqsHostConfiguration hostConfiguration, IMessageNameFormatter messageNameFormatter,
        IAmazonSqsTopologyConfiguration configuration)
        : base(hostConfiguration, configuration)
    {
        _hostConfiguration = hostConfiguration;
        _messageNameFormatter = messageNameFormatter;
        _configuration = configuration;
    }

    IAmazonSqsPublishTopology IAmazonSqsBusTopology.PublishTopology => _configuration.Publish;
    IAmazonSqsSendTopology IAmazonSqsBusTopology.SendTopology => _configuration.Send;

    IAmazonSqsMessagePublishTopology<T> IAmazonSqsBusTopology.Publish<T>()
    {
        return _configuration.Publish.GetMessageTopology<T>();
    }

    IAmazonSqsMessageSendTopology<T> IAmazonSqsBusTopology.Send<T>()
    {
        return _configuration.Send.GetMessageTopology<T>();
    }

    /// <summary>Resolves queue or topic send settings for an endpoint address.</summary>
    /// <param name="address">The absolute or host-relative destination address.</param>
    /// <returns>The resolved entity send settings.</returns>
    public SendSettings GetSendSettings(Uri address)
    {
        var endpointAddress = new AmazonSqsEndpointAddress(_hostConfiguration.HostAddress, address);

        return _configuration.Send.GetSendSettings(endpointAddress);
    }

    /// <summary>Creates an Amazon SNS destination address for a named topic.</summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="configure">An optional callback that changes topic attributes, tags, or lifetime.</param>
    /// <returns>The configured topic address.</returns>
    public Uri GetDestinationAddress(string topicName, Action<IAmazonSqsTopicConfigurator>? configure = null)
    {
        var address = new AmazonSqsEndpointAddress(
            _hostConfiguration.HostAddress,
            topicName,
            type: AmazonSqsEndpointAddress.AddressType.Topic);

        var publishSettings = new TopicPublishSettings(address);

        configure?.Invoke(publishSettings);

        return publishSettings.GetSendAddress(_hostConfiguration.HostAddress);
    }

    /// <summary>Creates an Amazon SNS destination address for a message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <param name="configure">An optional callback that changes topic attributes, tags, or lifetime.</param>
    /// <returns>The configured topic address.</returns>
    public Uri GetDestinationAddress(Type messageType, Action<IAmazonSqsTopicConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(messageType);
        if (messageType.ContainsGenericParameters)
            throw new ArgumentException("An open generic type cannot be used as a message name", nameof(messageType));

        Uri hostAddress = _hostConfiguration.HostAddress;
        AmazonSqsEndpointAddress address;
        if (_configuration.Publish.GetMessageTopology(messageType).TryGetPublishAddress(hostAddress, out Uri? publishAddress))
            address = new AmazonSqsEndpointAddress(hostAddress, publishAddress);
        else
        {
            var topicName = _messageNameFormatter.GetMessageName(messageType);
            var isTemporary = MessageTypeCache.IsTemporaryMessageType(messageType);
            address = new AmazonSqsEndpointAddress(hostAddress, topicName, durable: !isTemporary,
                autoDelete: isTemporary, type: AmazonSqsEndpointAddress.AddressType.Topic);
        }

        var publishSettings = new TopicPublishSettings(address);

        configure?.Invoke(publishSettings);

        return publishSettings.GetSendAddress(hostAddress);
    }
}
