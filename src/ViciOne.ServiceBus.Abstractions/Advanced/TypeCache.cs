using System;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Caches stable display names for runtime types without extending their lifetime.</summary>
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

    /// <summary>Gets the namespace-qualified display name of a runtime type.</summary>
    /// <param name="type">The runtime type to describe.</param>
    /// <returns>The cached display name.</returns>
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


/// <summary>Caches reflection metadata for a compile-time type.</summary>
/// <typeparam name="T">The type whose metadata is cached.</typeparam>
public sealed class TypeCache<T> :
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

    /// <summary>Gets cached readable public properties.</summary>
    public static IReadOnlyPropertyCache<T> ReadOnlyPropertyCache => Cached.Metadata.Value.ReadOnlyPropertyCache;
    /// <summary>Gets cached public properties that can be read and written.</summary>
    public static IReadWritePropertyCache<T> ReadWritePropertyCache => Cached.Metadata.Value.ReadWritePropertyCache;

    /// <summary>Gets the namespace-qualified display name of <typeparamref name="T" />.</summary>
    public static string ShortName => Cached.Metadata.Value.ShortName;

    IReadOnlyPropertyCache<T> ITypeCache<T>.ReadOnlyPropertyCache => _readPropertyCache.Value;
    IReadWritePropertyCache<T> ITypeCache<T>.ReadWritePropertyCache => _writePropertyCache.Value;
    string ITypeCache<T>.ShortName => _shortName;


    static class Cached
    {
        internal static readonly Lazy<ITypeCache<T>> Metadata = new(() => new TypeCache<T>());
    }
}
