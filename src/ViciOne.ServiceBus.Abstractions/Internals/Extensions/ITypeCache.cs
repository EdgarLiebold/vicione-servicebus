using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Internals;

internal interface ITypeCache<T>
{
    string ShortName { get; }
    IReadOnlyPropertyCache<T> ReadOnlyPropertyCache { get; }
    IReadWritePropertyCache<T> ReadWritePropertyCache { get; }
}
