namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates saga connector instances.</summary>
public interface ISagaConnectorFactory
{
    /// <summary>Creates message connector.</summary>
    /// <typeparam name="T">The saga state type.</typeparam>
    /// <returns>The created message connector.</returns>
    ISagaMessageConnector<T> CreateMessageConnector<T>()
        where T : class, ISaga;
}
