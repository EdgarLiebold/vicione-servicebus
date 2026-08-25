namespace ViciOne.ServiceBus
{
    using System;
    using System.Runtime.CompilerServices;
    using Internals;


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

        public static IReadOnlyPropertyCache<T> ReadOnlyPropertyCache => Cached.Metadata.Value.ReadOnlyPropertyCache;
        public static IReadWritePropertyCache<T> ReadWritePropertyCache => Cached.Metadata.Value.ReadWritePropertyCache;

        public static string ShortName => Cached.Metadata.Value.ShortName;

        IReadOnlyPropertyCache<T> ITypeCache<T>.ReadOnlyPropertyCache => _readPropertyCache.Value;
        IReadWritePropertyCache<T> ITypeCache<T>.ReadWritePropertyCache => _writePropertyCache.Value;
        string ITypeCache<T>.ShortName => _shortName;


        static class Cached
        {
            internal static readonly Lazy<ITypeCache<T>> Metadata = new Lazy<ITypeCache<T>>(() => new TypeCache<T>());
        }
    }
}
