namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates message connector instances.</summary>
public interface IMessageConnectorFactory
{
    /// <summary>Creates the message connector used with a factory-created consumer.</summary>
    /// <typeparam name="TConsumer">The consumer type requested by the caller.</typeparam>
    /// <returns>The connector typed for the requested consumer.</returns>
    IConsumerMessageConnector<TConsumer> CreateConsumerConnector<TConsumer>()
        where TConsumer : class;

    /// <summary>Creates the message connector used with an existing consumer instance.</summary>
    /// <typeparam name="TConsumer">The consumer type requested by the caller.</typeparam>
    /// <returns>The connector typed for the requested consumer.</returns>
    IInstanceMessageConnector<TConsumer> CreateInstanceConnector<TConsumer>()
        where TConsumer : class;
}
