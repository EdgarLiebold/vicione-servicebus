using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumers.Conventions;

namespace ViciOne.ServiceBus.Consumers.Metadata;

/// <summary>Provides the immutable message-contract snapshot discovered for a consumer type.</summary>
/// <typeparam name="TConsumer">The consumer type.</typeparam>
internal static class ConsumerMetadataCache<TConsumer>
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

                IMessageInterfaceType[] consumerTypes = CreateConsumerTypes(conventions);
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

    /// <summary>
    /// Builds an ordered descriptor snapshot in which a later convention replaces an earlier descriptor for the same message contract.
    /// </summary>
    /// <param name="conventions">The convention-registry snapshot to evaluate.</param>
    /// <returns>The validated message-contract descriptors.</returns>
    static IMessageInterfaceType[] CreateConsumerTypes(IConsumerConvention[] conventions)
    {
        var consumerTypes = new List<IMessageInterfaceType>();
        var positions = new Dictionary<Type, int>();

        foreach (IConsumerConvention convention in conventions)
        {
            string conventionName = TypeCache.GetShortName(convention.GetType());
            IConsumerMessageConvention messageConvention =
                ConsumerConventionCache.GetMessageConvention<TConsumer>(convention);
            IEnumerable<IMessageInterfaceType> messageTypes = messageConvention.GetMessageTypes()
                ?? throw new InvalidOperationException(
                    $"Consumer convention '{conventionName}' returned no MessageTypes collection "
                    + $"for consumer '{TypeCache<TConsumer>.ShortName}'.");

            foreach (IMessageInterfaceType? descriptor in messageTypes)
            {
                if (descriptor is null)
                {
                    throw new InvalidOperationException(
                        $"Consumer convention '{conventionName}' returned a null MessageDescriptor "
                        + $"for consumer '{TypeCache<TConsumer>.ShortName}'.");
                }

                Type messageType = descriptor.MessageType
                    ?? throw new InvalidOperationException(
                        $"Consumer convention '{conventionName}' returned a MessageDescriptor with no MessageType "
                        + $"for consumer '{TypeCache<TConsumer>.ShortName}'.");

                if (positions.TryGetValue(messageType, out int position))
                    consumerTypes[position] = descriptor;
                else
                {
                    positions.Add(messageType, consumerTypes.Count);
                    consumerTypes.Add(descriptor);
                }
            }
        }

        return consumerTypes.ToArray();
    }
}
