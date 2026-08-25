namespace ViciOne.ServiceBus.RabbitMqTransport.Topology
{
    using System;
    using Configuration;
    using Transports;


    public class RabbitMqBusTopology :
        BusTopology,
        IRabbitMqBusTopology
    {
        readonly IRabbitMqTopologyConfiguration _configuration;
        readonly IRabbitMqHostConfiguration _hostConfiguration;
        readonly IMessageNameFormatter _messageNameFormatter;

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

        public Uri GetDestinationAddress(string exchangeName, Action<IRabbitMqExchangeConfigurator> configure = null)
        {
            var hostAddress = _hostConfiguration.HostAddress;
            var address = new RabbitMqEndpointAddress(hostAddress, exchangeName);

            var sendSettings = new RabbitMqSendSettings(address);

            configure?.Invoke(sendSettings);

            return sendSettings.GetSendAddress(hostAddress);
        }

        public Uri GetDestinationAddress(Type messageType, Action<IRabbitMqExchangeConfigurator> configure = null)
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
}
