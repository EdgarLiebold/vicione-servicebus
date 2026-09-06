using System.Reflection;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Applies conventions for initializer.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public abstract class InitializerConvention<TMessage> :
    IInitializerConvention<TMessage>
    where TMessage : class
{
    readonly IConventionTypeCache<IMessageInputInitializerConvention<TMessage>> _typeCache;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="cacheFactory">The cache factory.</param>
    /// <param name="convention">The convention.</param>
    protected InitializerConvention(IConventionTypeCacheFactory<IMessageInputInitializerConvention<TMessage>> cacheFactory, IInitializerConvention
        convention)
    {
        _typeCache = new ConventionTypeCache<IMessageInputInitializerConvention<TMessage>>(cacheFactory, convention);
    }

    /// <summary>Attempts to get property initializer.</summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPropertyInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        where TInput : class
    {
        return _typeCache.GetOrAdd<TInput, IInitializerConvention<TMessage, TInput>>().TryGetPropertyInitializer<TProperty>(propertyInfo, out initializer);
    }

    /// <summary>Attempts to get header initializer.</summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeaderInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TInput : class
    {
        return _typeCache.GetOrAdd<TInput, IInitializerConvention<TMessage, TInput>>().TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);
    }

    /// <summary>Attempts to get headers initializer.</summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeadersInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TInput : class
    {
        return _typeCache.GetOrAdd<TInput, IInitializerConvention<TMessage, TInput>>().TryGetHeadersInitializer<TProperty>(propertyInfo, out initializer);
    }


    /// <summary>Represents an initializer convention that cannot map the selected input type.</summary>
    /// <typeparam name="TInput">The input type.</typeparam>
    protected class Unsupported<TInput> :
        IInitializerConvention<TMessage, TInput>
        where TInput : class
    {
        /// <summary>Attempts to get property initializer.</summary>
        /// <typeparam name="TProperty">The property type.</typeparam>
        /// <param name="propertyInfo">The property info.</param>
        /// <param name="initializer">Receives the initializer produced by the operation.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool TryGetPropertyInitializer<TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        {
            initializer = default;
            return false;
        }

        /// <summary>Attempts to get header initializer.</summary>
        /// <typeparam name="TProperty">The property type.</typeparam>
        /// <param name="propertyInfo">The property info.</param>
        /// <param name="initializer">Receives the initializer produced by the operation.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool TryGetHeaderInitializer<TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        {
            initializer = default;
            return false;
        }

        /// <summary>Attempts to get headers initializer.</summary>
        /// <typeparam name="TProperty">The property type.</typeparam>
        /// <param name="propertyInfo">The property info.</param>
        /// <param name="initializer">Receives the initializer produced by the operation.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool TryGetHeadersInitializer<TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        {
            initializer = default;
            return false;
        }
    }
}


/// <summary>Applies conventions for initializer.</summary>
public abstract class InitializerConvention :
    IInitializerConvention
{
    readonly IConventionTypeCache<IMessageInitializerConvention> _typeCache;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="cacheFactory">The cache factory.</param>
    protected InitializerConvention(IConventionTypeCacheFactory<IMessageInitializerConvention> cacheFactory)
    {
        _typeCache = new ConventionTypeCache<IMessageInitializerConvention>(cacheFactory, this);
    }

    /// <summary>Attempts to get property initializer.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPropertyInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class
    {
        return _typeCache.GetOrAdd<TMessage, IInitializerConvention<TMessage>>()
            .TryGetPropertyInitializer<TInput, TProperty>(propertyInfo, out initializer);
    }

    /// <summary>Attempts to get header initializer.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeaderInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class
    {
        return _typeCache.GetOrAdd<TMessage, IInitializerConvention<TMessage>>().TryGetHeaderInitializer<TInput, TProperty>(propertyInfo, out initializer);
    }

    /// <summary>Attempts to get headers initializer.</summary>
    /// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeadersInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class
    {
        return _typeCache.GetOrAdd<TMessage, IInitializerConvention<TMessage>>().TryGetHeadersInitializer<TInput, TProperty>(propertyInfo, out initializer);
    }
}
