using System;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Provides a type cache implementation.
/// </summary>
public static class TypeCache
{
    static CachedType GetOrAdd(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return Cached.Instance.GetValue(type, static value => new CachedType(value.GetTypeName()));
    }

    internal static void GetOrAdd<T>(Type type, ITypeCache<T> typeCache)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(typeCache);

        Cached.Instance.GetValue(type, _ => new CachedType(typeCache.ShortName));
    }

    /// <summary>
    /// Gets short name.
    /// </summary>
    /// <param name="type">The type value.</param>
    /// <returns>The result of the operation.</returns>
    public static string GetShortName(Type type)
    {
        return GetOrAdd(type).ShortName;
    }

    static class Cached
    {
        internal static readonly ConditionalWeakTable<Type, CachedType> Instance = new();
    }


    sealed class CachedType
    {
        public CachedType(string shortName)
        {
            ShortName = shortName;
        }

        public string ShortName { get; }
    }
}


/// <summary>
/// Provides a type cache implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class TypeCache<T> :
    ITypeCache<T>
{
    readonly Lazy<ReadOnlyPropertyCache<T>> _readPropertyCache;
    readonly string _shortName;
    readonly Lazy<ReadWritePropertyCache<T>> _writePropertyCache;

    TypeCache()
    {
        _shortName = typeof(T).GetTypeName();
        _readPropertyCache = new Lazy<ReadOnlyPropertyCache<T>>(() => new ReadOnlyPropertyCache<T>());
        _writePropertyCache = new Lazy<ReadWritePropertyCache<T>>(() => new ReadWritePropertyCache<T>());

        TypeCache.GetOrAdd(typeof(T), this);
    }

    /// <summary>
    /// Gets the read only property cache value.
    /// </summary>
    public static IReadOnlyPropertyCache<T> ReadOnlyPropertyCache => Cached.Metadata.Value.ReadOnlyPropertyCache;
    /// <summary>
    /// Gets the read write property cache value.
    /// </summary>
    public static IReadWritePropertyCache<T> ReadWritePropertyCache => Cached.Metadata.Value.ReadWritePropertyCache;

    /// <summary>
    /// Gets the short name value.
    /// </summary>
    public static string ShortName => Cached.Metadata.Value.ShortName;

    IReadOnlyPropertyCache<T> ITypeCache<T>.ReadOnlyPropertyCache => _readPropertyCache.Value;
    IReadWritePropertyCache<T> ITypeCache<T>.ReadWritePropertyCache => _writePropertyCache.Value;
    string ITypeCache<T>.ShortName => _shortName;


    static class Cached
    {
        internal static readonly Lazy<ITypeCache<T>> Metadata = new Lazy<ITypeCache<T>>(() => new TypeCache<T>());
    }
}
