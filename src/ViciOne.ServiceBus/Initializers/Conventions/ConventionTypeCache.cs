using System;
using System.Collections.Concurrent;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Caches one lazily created convention adapter per closed contract type.</summary>
internal sealed class ConventionTypeCache :
    IConventionTypeCache
{
    readonly IInitializerConvention _convention;
    readonly ConcurrentDictionary<Type, Cached> _dictionary;
    readonly IConventionTypeCacheFactory _typeFactory;

    public ConventionTypeCache(IConventionTypeCacheFactory typeFactory, IInitializerConvention convention)
    {
        _typeFactory = typeFactory ?? throw new ArgumentNullException(nameof(typeFactory));
        _convention = convention ?? throw new ArgumentNullException(nameof(convention));

        _dictionary = new ConcurrentDictionary<Type, Cached>();
    }

    TResult IConventionTypeCache.GetOrAdd<T, TResult>()
    {
        var result = _dictionary.GetOrAdd(typeof(T), add => new CachedValue(() => _typeFactory.Create<T>(_convention))).Value as TResult;
        if (result == null)
            throw new ArgumentException($"The specified result type was invalid: {TypeCache<TResult>.ShortName}");

        return result;
    }


    interface Cached
    {
        object Value { get; }
    }


    sealed class CachedValue :
        Cached
    {
        readonly Lazy<object> _value;

        public CachedValue(Func<object> valueFactory)
        {
            _value = new Lazy<object>(valueFactory);
        }

        public object Value => _value.Value;
    }
}
