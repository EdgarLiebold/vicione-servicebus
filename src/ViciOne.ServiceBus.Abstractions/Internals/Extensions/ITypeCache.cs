namespace ViciOne.ServiceBus.Internals
{
    using Metadata;


    internal interface ITypeCache<T>
    {
        string ShortName { get; }
        IReadOnlyPropertyCache<T> ReadOnlyPropertyCache { get; }
        IReadWritePropertyCache<T> ReadWritePropertyCache { get; }
    }
}
