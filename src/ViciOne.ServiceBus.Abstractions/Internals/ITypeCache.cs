using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Internals;

/// <summary>Exposes the lazily created name and property metadata for a runtime type.</summary>
/// <typeparam name="T">The type whose metadata is cached.</typeparam>
internal interface ITypeCache<T>
{
    /// <summary>Gets the namespace-qualified display name.</summary>
    string ShortName { get; }

    /// <summary>Gets the readable public-property cache.</summary>
    IReadOnlyPropertyCache<T> ReadOnlyPropertyCache { get; }

    /// <summary>Gets the readable and writable public-property cache.</summary>
    IReadWritePropertyCache<T> ReadWritePropertyCache { get; }
}
