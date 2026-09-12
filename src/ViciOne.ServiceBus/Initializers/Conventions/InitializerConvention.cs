using System;
using System.Reflection;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Dispatches convention mapping requests by input type for one message contract.</summary>
internal abstract class InitializerConvention<TMessage> :
    IInitializerConvention<TMessage>
    where TMessage : class
{
    readonly IConventionTypeCache _typeCache;

    protected InitializerConvention(IConventionTypeCacheFactory cacheFactory, IInitializerConvention convention)
    {
        ArgumentNullException.ThrowIfNull(cacheFactory);
        ArgumentNullException.ThrowIfNull(convention);
        _typeCache = new ConventionTypeCache(cacheFactory, convention);
    }

    public bool TryGetPropertyInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        where TInput : class
    {
        return _typeCache.GetOrAdd<TInput, IInitializerConvention<TMessage, TInput>>().TryGetPropertyInitializer<TProperty>(propertyInfo, out initializer);
    }

    public bool TryGetHeaderInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TInput : class
    {
        return _typeCache.GetOrAdd<TInput, IInitializerConvention<TMessage, TInput>>().TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);
    }

    public bool TryGetHeadersInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TInput : class
    {
        return _typeCache.GetOrAdd<TInput, IInitializerConvention<TMessage, TInput>>().TryGetHeadersInitializer<TProperty>(propertyInfo, out initializer);
    }


    /// <summary>Represents a convention that does not support the selected input type.</summary>
    protected sealed class Unsupported<TInput> :
        IInitializerConvention<TMessage, TInput>
        where TInput : class
    {
        public bool TryGetPropertyInitializer<TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        {
            initializer = default;
            return false;
        }

        public bool TryGetHeaderInitializer<TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        {
            initializer = default;
            return false;
        }

        public bool TryGetHeadersInitializer<TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        {
            initializer = default;
            return false;
        }
    }
}


/// <summary>Dispatches convention mapping requests by message and input contract type.</summary>
internal abstract class InitializerConvention :
    IInitializerConvention
{
    readonly IConventionTypeCache _typeCache;

    protected InitializerConvention(IConventionTypeCacheFactory cacheFactory)
    {
        ArgumentNullException.ThrowIfNull(cacheFactory);
        _typeCache = new ConventionTypeCache(cacheFactory, this);
    }

    public bool TryGetPropertyInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class
    {
        return _typeCache.GetOrAdd<TMessage, IInitializerConvention<TMessage>>()
            .TryGetPropertyInitializer<TInput, TProperty>(propertyInfo, out initializer);
    }

    public bool TryGetHeaderInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class
    {
        return _typeCache.GetOrAdd<TMessage, IInitializerConvention<TMessage>>().TryGetHeaderInitializer<TInput, TProperty>(propertyInfo, out initializer);
    }

    public bool TryGetHeadersInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class
    {
        return _typeCache.GetOrAdd<TMessage, IInitializerConvention<TMessage>>().TryGetHeadersInitializer<TInput, TProperty>(propertyInfo, out initializer);
    }
}
