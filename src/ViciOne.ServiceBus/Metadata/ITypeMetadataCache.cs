// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Metadata
{
    using System;


    public interface ITypeMetadataCache<out T>
    {
        /// <summary>
        /// The implementation type for the type, if it's an interface
        /// </summary>
        Type ImplementationType { get; }
    }
}
