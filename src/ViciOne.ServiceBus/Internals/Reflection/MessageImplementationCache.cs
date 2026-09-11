using System;

namespace ViciOne.ServiceBus.Internals.Reflection;

/// <summary>Caches the concrete runtime type used to materialize an interface message contract.</summary>
internal static class MessageImplementationCache
{
    /// <summary>Gets the implementation builder shared by message initializers and serializers.</summary>
    internal static IImplementationBuilder Builder => DynamicImplementationBuilder.Instance;

    /// <summary>Returns the generated implementation of an interface message contract.</summary>
    /// <param name="type">The interface message contract to materialize.</param>
    /// <returns>The concrete runtime type used for instances of the interface.</returns>
    internal static Type GetImplementationType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        if (!type.IsInterface)
            throw new ArgumentException("Only interface message contracts require generated implementations.", nameof(type));

        return Builder.GetImplementationType(type);
    }
}

/// <summary>Caches the concrete runtime type for one compile-time interface message contract.</summary>
/// <typeparam name="TMessage">The interface message contract to materialize.</typeparam>
internal static class MessageImplementationCache<TMessage>
{
    static readonly Lazy<Type> ImplementationTypeCache = new(
        () => MessageImplementationCache.GetImplementationType(typeof(TMessage)));

    /// <summary>Gets the concrete runtime type used for <typeparamref name="TMessage" />.</summary>
    internal static Type ImplementationType => ImplementationTypeCache.Value;
}
