using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the immutable message-contract snapshot discovered for a consumer type.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
public static class ConsumerMetadataCache<TConsumer>
    where TConsumer : class
{
    static readonly object CacheLock = new();
    static IReadOnlyList<IMessageInterfaceType> _consumerTypes = [];
    static long _version = -1;

    /// <summary>Gets an immutable snapshot for the current consumer-convention version.</summary>
    public static IReadOnlyList<IMessageInterfaceType> ConsumerTypes
    {
        get
        {
            (long version, IConsumerConvention[] conventions) = ConsumerConventionCache.GetSnapshot();
            lock (CacheLock)
            {
                if (_version == version)
                    return _consumerTypes;

                IMessageInterfaceType[] consumerTypes = conventions
                    .Select(convention => convention.GetConsumerMessageConvention<TConsumer>())
                    .SelectMany(convention => convention.GetMessageTypes())
                    .GroupBy(type => type.MessageType)
                    .Select(group => group.Last())
                    .ToArray();
                _consumerTypes = Array.AsReadOnly(consumerTypes);
                _version = version;
                return _consumerTypes;
            }
        }
    }

    internal static long Version
    {
        get
        {
            _ = ConsumerTypes;
            lock (CacheLock)
                return _version;
        }
    }
}
