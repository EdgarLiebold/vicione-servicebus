using System;
using ViciOne.ServiceBus.AmazonSqs.Configuration;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.AmazonSqs.Topology;

/// <summary>
/// Provides an amazon sqs bus topology implementation.
/// </summary>
public class AmazonSqsBusTopology :
    BusTopology,
    IAmazonSqsBusTopology
{
    readonly IAmazonSqsTopologyConfiguration _configuration;
    readonly IAmazonSqsHostConfiguration _hostConfiguration;
    readonly IMessageNameFormatter _messageNameFormatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="hostConfiguration">The host configuration value.</param>
    /// <param name="messageNameFormatter">The message name formatter value.</param>
    /// <param name="configuration">The configuration callback.</param>
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

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="address">The address value.</param>
    /// <returns>The result of the operation.</returns>
    public SendSettings GetSendSettings(Uri address)
    {
        var endpointAddress = new AmazonSqsEndpointAddress(_hostConfiguration.HostAddress, address);

        return _configuration.Send.GetSendSettings(endpointAddress);
    }

    /// <summary>
    /// Gets destination address.
    /// </summary>
    /// <param name="topicName">The topic name value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Gets destination address.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <param name="configure">The configuration callback.</param>
    /// <returns>The result of the operation.</returns>
    public Uri GetDestinationAddress(Type messageType, Action<IAmazonSqsTopicConfigurator>? configure = null)
    {
        var topicName = _messageNameFormatter.GetMessageName(messageType);
        var isTemporary = MessageTypeCache.IsTemporaryMessageType(messageType);
        var address = new AmazonSqsEndpointAddress(
            _hostConfiguration.HostAddress,
            topicName,
            durable: !isTemporary,
            autoDelete: isTemporary,
            type: AmazonSqsEndpointAddress.AddressType.Topic);

        var publishSettings = new TopicPublishSettings(address);

        configure?.Invoke(publishSettings);

        return publishSettings.GetSendAddress(_hostConfiguration.HostAddress);
    }
}
