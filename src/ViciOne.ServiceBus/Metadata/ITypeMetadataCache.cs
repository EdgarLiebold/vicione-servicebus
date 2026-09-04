using System;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>
/// Defines the contract for type metadata cache.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ITypeMetadataCache<out T>
{
    /// <summary>
    /// The implementation type for the type, if it's an interface
    /// </summary>
    Type ImplementationType { get; }
}
