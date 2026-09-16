using System;
using System.Runtime.CompilerServices;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Caches one lazily created convention adapter per live closed contract type.</summary>
/// <remarks>Collectible contract types and their closed adapters are released together when the caller no longer owns the type.</remarks>
internal sealed class ConventionTypeCache :
    IConventionTypeCache
{
    readonly IInitializerConvention _convention;
    readonly ConditionalWeakTable<Type, Cached> _dictionary;
    readonly IConventionTypeCacheFactory _typeFactory;

    public ConventionTypeCache(IConventionTypeCacheFactory typeFactory, IInitializerConvention convention)
    {
        _typeFactory = typeFactory ?? throw new ArgumentNullException(nameof(typeFactory));
        _convention = convention ?? throw new ArgumentNullException(nameof(convention));

        _dictionary = new ConditionalWeakTable<Type, Cached>();
    }

    TResult IConventionTypeCache.GetOrAdd<T, TResult>()
    {
        var result = _dictionary.GetValue(typeof(T), _ => new CachedValue(() => _typeFactory.Create<T>(_convention))).Value as TResult;
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
