namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Defines the operations required by in memory bus topology.</summary>
public interface IInMemoryBusTopology :
    IBusTopology
{
    /// <summary>Publishes a message to its configured consumers.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The in memory message publish topology produced by the operation.</returns>
    new IInMemoryMessagePublishTopology<T> Publish<T>()
        where T : class;
}
