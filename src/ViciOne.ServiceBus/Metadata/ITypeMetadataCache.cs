using System;

namespace ViciOne.ServiceBus.Metadata;

public interface ITypeMetadataCache<out T>
{
    /// <summary>
    /// The implementation type for the type, if it's an interface
    /// </summary>
    Type ImplementationType { get; }
}
