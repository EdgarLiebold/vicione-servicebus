namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>Provides send settings for the RabbitMQ exchange used to delay a message.</summary>
public class RabbitMqDelaySettings :
    RabbitMqSendSettings,
    DelaySettings
{
    /// <summary>Creates delay-exchange settings from a RabbitMQ endpoint address.</summary>
    /// <param name="address">The delayed-message exchange address.</param>
    public RabbitMqDelaySettings(RabbitMqEndpointAddress address)
        : base(address)
    {
    }
}
