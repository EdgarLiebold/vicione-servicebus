using System;

namespace ViciOne.ServiceBus.Metadata;

/// <summary>Provides cached access to type metadata data.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ITypeMetadataCache<out T>
{
    /// <summary>The implementation type for the type, if it's an interface.</summary>
    Type ImplementationType { get; }
}
