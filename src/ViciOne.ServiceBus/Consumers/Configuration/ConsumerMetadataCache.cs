using System;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a consumer metadata cache implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class ConsumerMetadataCache<T> :
    IConsumerMetadataCache<T>
    where T : class
{
    readonly IMessageInterfaceType[] _consumerTypes;

    ConsumerMetadataCache()
    {
        _consumerTypes = ConsumerConventionCache.GetConventions<T>()
            .SelectMany(x => x.GetMessageTypes())
            .GroupBy(x => x.MessageType)
            .Select(x => x.Last())
            .ToArray();
    }

    /// <summary>
    /// Gets the consumer types value.
    /// </summary>
    public static IMessageInterfaceType[] ConsumerTypes => Cached.Metadata.Value.ConsumerTypes;

    IMessageInterfaceType[] IConsumerMetadataCache<T>.ConsumerTypes => _consumerTypes;


    static class Cached
    {
        internal static readonly Lazy<IConsumerMetadataCache<T>> Metadata = new Lazy<IConsumerMetadataCache<T>>(() => new ConsumerMetadataCache<T>());
    }
}
