namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Defines the operations required by message exchange.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IMessageExchange<T> :
    IMessageSink<T>,
    IMessageSource<T>
    where T : class
{
    /// <summary>Gets the name.</summary>
    string Name { get; }
}
