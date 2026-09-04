#nullable enable
namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>
/// Defines the contract for message exchange.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IMessageExchange<T> :
    IMessageSink<T>,
    IMessageSource<T>
    where T : class
{
    /// <summary>
    /// Gets the name value.
    /// </summary>
    string Name { get; }
}
