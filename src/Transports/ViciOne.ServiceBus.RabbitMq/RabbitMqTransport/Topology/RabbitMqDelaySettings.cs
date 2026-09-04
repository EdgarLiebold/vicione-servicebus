namespace ViciOne.ServiceBus.RabbitMq.Topology;

/// <summary>
/// Provides a rabbit mq delay settings implementation.
/// </summary>
public class RabbitMqDelaySettings :
    RabbitMqSendSettings,
    DelaySettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public RabbitMqDelaySettings(RabbitMqEndpointAddress address)
        : base(address)
    {
    }
}
