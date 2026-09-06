using System;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Carries metadata for registration.</summary>
public static class RegistrationMetadata
{
    /// <summary>Returns true if the type is a consumer, or a consumer definition.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsConsumerOrDefinition(Type type)
    {
        Type[] interfaces = type.GetInterfaces();

        return !IsConsumerRegistrationExcluded(type)
            && (typeof(IConsumer).IsAssignableFrom(type)
                || interfaces.Any(candidate => candidate.ImplementsInterface(typeof(IConsumerDefinition<>))));
    }

    /// <summary>Returns true if the type is a consumer, or a consumer definition.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public static bool IsConsumer(Type type)
    {
        return !IsConsumerRegistrationExcluded(type) && typeof(IConsumer).IsAssignableFrom(type);
    }

    /// <summary>Returns whether a capability package, rather than the core consumer kind, owns registration of the type.</summary>
    /// <param name="type">The runtime type to inspect or use.</param>
    /// <returns><see langword="true" /> when an implemented handler contract excludes core consumer registration.</returns>
    public static bool IsConsumerRegistrationExcluded(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        Type[] interfaces = type.GetInterfaces();
        return interfaces.Any(candidate =>
            candidate.IsDefined(typeof(ConsumerRegistrationExclusionAttribute), inherit: false));
    }

}
