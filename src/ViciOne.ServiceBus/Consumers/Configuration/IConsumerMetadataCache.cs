namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for consumer metadata cache.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface IConsumerMetadataCache<T>
{
    /// <summary>
    /// Gets the consumer types value.
    /// </summary>
    IMessageInterfaceType[] ConsumerTypes { get; }
}
