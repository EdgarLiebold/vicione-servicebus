using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides cached access to consumer metadata data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface IConsumerMetadataCache<T>
{
    /// <summary>Gets the consumer types.</summary>
    IReadOnlyList<IMessageInterfaceType> ConsumerTypes { get; }
}
