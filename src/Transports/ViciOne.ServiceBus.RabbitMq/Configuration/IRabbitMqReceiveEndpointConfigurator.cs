using System;
using ViciOne.ServiceBus.RabbitMq;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Configures a RabbitMQ receive endpoint and its broker topology.</summary>
public interface IRabbitMqReceiveEndpointConfigurator :
    IReceiveEndpointConfigurator,
    IRabbitMqQueueEndpointConfigurator
{
    /// <summary>Specifies whether deployment includes the endpoint queue and its exchange-to-queue binding.</summary>
    bool BindQueue { set; }

    /// <summary>Sets the exchange that receives messages dead-lettered by the endpoint queue.</summary>
    string DeadLetterExchange { set; }

    /// <summary>Binds an exchange to the receive endpoint exchange.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="callback">An optional callback that customizes the source exchange and binding.</param>
    void Bind(string exchangeName, Action<IRabbitMqExchangeToExchangeBindingConfigurator>? callback = null);

    /// <summary>Binds the publish exchange for a message contract to the receive endpoint exchange.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="callback">An optional callback that customizes the message exchange and binding.</param>
    void Bind<T>(Action<IRabbitMqExchangeBindingConfigurator>? callback = null)
        where T : class;

    /// <summary>Declares and binds an exchange and queue that receive messages dead-lettered by the endpoint queue.</summary>
    /// <param name="exchangeName">The exchange name.</param>
    /// <param name="queueName">The queue name.</param>
    /// <param name="configure">An optional callback that customizes the dead-letter queue and binding.</param>
    void BindDeadLetterQueue(string exchangeName, string? queueName = null, Action<IRabbitMqQueueBindingConfigurator>? configure = null);

    /// <summary>Adds middleware to the RabbitMQ channel pipeline.</summary>
    /// <param name="configure">The channel-pipeline configuration callback.</param>
    void ConfigureChannel(Action<IPipeConfigurator<ChannelContext>> configure);

    /// <summary>Adds middleware to the RabbitMQ connection pipeline.</summary>
    /// <param name="configure">The connection-pipeline configuration callback.</param>
    void ConfigureConnection(Action<IPipeConfigurator<ConnectionContext>> configure);

    /// <summary>Overrides RabbitMQ's dynamically generated consumer tag for this endpoint.</summary>
    /// <param name="consumerTag">The consumer tag to use for this receive endpoint.</param>
    void OverrideConsumerTag(string consumerTag);

    /// <summary>Configures the receive endpoint queue as a RabbitMQ stream.</summary>
    /// <param name="callback">An optional callback that configures stream retention and consumer offset.</param>
    void Stream(Action<IRabbitMqStreamConfigurator>? callback = null);

    /// <summary>Configures the receive endpoint queue as a RabbitMQ stream with an explicit consumer tag.</summary>
    /// <param name="consumerTag">Overrides the default consumer tag with the specified name.</param>
    /// <param name="callback">An optional callback that configures stream retention and consumer offset.</param>
    void Stream(string consumerTag, Action<IRabbitMqStreamConfigurator>? callback = null);

    /// <summary>
    /// Sets RabbitMQ's delivery-acknowledgement timeout for the endpoint queue.
    /// <see href="https://www.rabbitmq.com/docs/consumers#acknowledgement-timeout"/>
    /// </summary>
    /// <param name="timeSpan">The maximum time allowed before a delivery must be acknowledged, in whole milliseconds.</param>
    void SetDeliveryAcknowledgementTimeout(TimeSpan timeSpan);

    /// <summary>
    /// Sets RabbitMQ's delivery-acknowledgement timeout from optional duration components.
    /// <see href="https://www.rabbitmq.com/docs/consumers#acknowledgement-timeout"/>
    /// </summary>
    /// <param name="d">The optional number of days.</param>
    /// <param name="h">The optional number of hours.</param>
    /// <param name="m">The optional number of minutes.</param>
    /// <param name="s">The optional number of seconds.</param>
    /// <param name="ms">The optional number of milliseconds.</param>
    void SetDeliveryAcknowledgementTimeout(int? d = null, int? h = null, int? m = null, int? s = null, int? ms = null);
}
