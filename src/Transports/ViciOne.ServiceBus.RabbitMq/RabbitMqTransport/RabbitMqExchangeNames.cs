namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Defines RabbitMQ-reserved entity names used by the transport.</summary>
public static class RabbitMqExchangeNames
{
    /// <summary>The RabbitMQ direct-reply-to pseudo-queue name.</summary>
    public const string ReplyTo = "amq.rabbitmq.reply-to";
}
