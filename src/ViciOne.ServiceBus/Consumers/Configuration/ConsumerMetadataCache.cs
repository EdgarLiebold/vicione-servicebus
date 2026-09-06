using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Caches consumer metadata data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class ConsumerMetadataCache<T> :
    IConsumerMetadataCache<T>
    where T : class
{
    readonly IReadOnlyList<IMessageInterfaceType> _consumerTypes;

    ConsumerMetadataCache()
    {
        IMessageInterfaceType[] consumerTypes = ConsumerConventionCache.GetConventions<T>()
            .SelectMany(x => x.GetMessageTypes())
            .GroupBy(x => x.MessageType)
            .Select(x => x.Last())
            .ToArray();
        _consumerTypes = Array.AsReadOnly(consumerTypes);
    }

    /// <summary>Gets the consumer types.</summary>
    public static IReadOnlyList<IMessageInterfaceType> ConsumerTypes => Cached.Metadata.Value.ConsumerTypes;

    IReadOnlyList<IMessageInterfaceType> IConsumerMetadataCache<T>.ConsumerTypes => _consumerTypes;


    static class Cached
    {
        internal static readonly Lazy<IConsumerMetadataCache<T>> Metadata = new Lazy<IConsumerMetadataCache<T>>(() => new ConsumerMetadataCache<T>());
    }
}
