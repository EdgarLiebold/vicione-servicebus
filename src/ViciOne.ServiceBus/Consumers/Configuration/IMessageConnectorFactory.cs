namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates message connector instances.</summary>
public interface IMessageConnectorFactory
{
    /// <summary>Creates consumer connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created consumer connector.</returns>
    IConsumerMessageConnector<T> CreateConsumerConnector<T>()
        where T : class;

    /// <summary>Creates instance connector.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The created instance connector.</returns>
    IInstanceMessageConnector<T> CreateInstanceConnector<T>()
        where T : class;
}
