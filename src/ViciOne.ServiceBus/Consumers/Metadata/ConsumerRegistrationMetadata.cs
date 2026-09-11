using System;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Consumers.Metadata;

/// <summary>Classifies runtime types for convention-based consumer registration.</summary>
internal static class ConsumerRegistrationMetadata
{
    /// <summary>Determines whether a type is owned by core consumer or consumer-definition registration.</summary>
    /// <param name="type">The runtime type to classify.</param>
    /// <returns><see langword="true" /> for a non-excluded consumer or consumer definition; otherwise, <see langword="false" />.</returns>
    internal static bool IsConsumerOrDefinition(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type[] interfaces = type.GetInterfaces();

        return !IsConsumerRegistrationExcluded(type)
            && (typeof(IConsumer).IsAssignableFrom(type)
                || interfaces.Any(candidate => candidate.ImplementsInterface(typeof(IConsumerDefinition<>))));
    }

    /// <summary>Determines whether a type is owned by core consumer registration.</summary>
    /// <param name="type">The runtime type to classify.</param>
    /// <returns><see langword="true" /> for a non-excluded consumer; otherwise, <see langword="false" />.</returns>
    internal static bool IsConsumer(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return !IsConsumerRegistrationExcluded(type) && typeof(IConsumer).IsAssignableFrom(type);
    }

    /// <summary>Returns whether a capability package, rather than the core consumer kind, owns registration of the type.</summary>
    /// <param name="type">The runtime type to classify.</param>
    /// <returns><see langword="true" /> when an implemented handler contract excludes core consumer registration.</returns>
    internal static bool IsConsumerRegistrationExcluded(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type[] interfaces = type.GetInterfaces();
        return interfaces.Any(candidate =>
            candidate.IsDefined(typeof(ConsumerRegistrationExclusionAttribute), inherit: false));
    }
}
