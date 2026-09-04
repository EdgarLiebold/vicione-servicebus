using System.Reflection;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>
/// Provides an initializer convention implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public abstract class InitializerConvention<TMessage> :
    IInitializerConvention<TMessage>
    where TMessage : class
{
    readonly IConventionTypeCache<IMessageInputInitializerConvention<TMessage>> _typeCache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="cacheFactory">The cache factory value.</param>
    /// <param name="convention">The convention value.</param>
    protected InitializerConvention(IConventionTypeCacheFactory<IMessageInputInitializerConvention<TMessage>> cacheFactory, IInitializerConvention
        convention)
    {
        _typeCache = new ConventionTypeCache<IMessageInputInitializerConvention<TMessage>>(cacheFactory, convention);
    }

    /// <summary>
    /// Attempts to get property initializer.
    /// </summary>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPropertyInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        where TInput : class
    {
        return _typeCache.GetOrAdd<TInput, IInitializerConvention<TMessage, TInput>>().TryGetPropertyInitializer<TProperty>(propertyInfo, out initializer);
    }

    /// <summary>
    /// Attempts to get header initializer.
    /// </summary>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeaderInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TInput : class
    {
        return _typeCache.GetOrAdd<TInput, IInitializerConvention<TMessage, TInput>>().TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);
    }

    /// <summary>
    /// Attempts to get headers initializer.
    /// </summary>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeadersInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TInput : class
    {
        return _typeCache.GetOrAdd<TInput, IInitializerConvention<TMessage, TInput>>().TryGetHeadersInitializer<TProperty>(propertyInfo, out initializer);
    }


    /// <summary>
    /// Provides an unsupported implementation.
    /// </summary>
    /// <typeparam name="TInput">The t input type.</typeparam>
    protected class Unsupported<TInput> :
        IInitializerConvention<TMessage, TInput>
        where TInput : class
    {
        /// <summary>
        /// Attempts to get property initializer.
        /// </summary>
        /// <typeparam name="TProperty">The t property type.</typeparam>
        /// <param name="propertyInfo">The property info value.</param>
        /// <param name="initializer">The initializer value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool TryGetPropertyInitializer<TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        {
            initializer = default;
            return false;
        }

        /// <summary>
        /// Attempts to get header initializer.
        /// </summary>
        /// <typeparam name="TProperty">The t property type.</typeparam>
        /// <param name="propertyInfo">The property info value.</param>
        /// <param name="initializer">The initializer value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool TryGetHeaderInitializer<TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        {
            initializer = default;
            return false;
        }

        /// <summary>
        /// Attempts to get headers initializer.
        /// </summary>
        /// <typeparam name="TProperty">The t property type.</typeparam>
        /// <param name="propertyInfo">The property info value.</param>
        /// <param name="initializer">The initializer value.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool TryGetHeadersInitializer<TProperty>(PropertyInfo propertyInfo,
            [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        {
            initializer = default;
            return false;
        }
    }
}


/// <summary>
/// Provides an initializer convention implementation.
/// </summary>
public abstract class InitializerConvention :
    IInitializerConvention
{
    readonly IConventionTypeCache<IMessageInitializerConvention> _typeCache;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="cacheFactory">The cache factory value.</param>
    protected InitializerConvention(IConventionTypeCacheFactory<IMessageInitializerConvention> cacheFactory)
    {
        _typeCache = new ConventionTypeCache<IMessageInitializerConvention>(cacheFactory, this);
    }

    /// <summary>
    /// Attempts to get property initializer.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPropertyInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class
    {
        return _typeCache.GetOrAdd<TMessage, IInitializerConvention<TMessage>>()
            .TryGetPropertyInitializer<TInput, TProperty>(propertyInfo, out initializer);
    }

    /// <summary>
    /// Attempts to get header initializer.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeaderInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class
    {
        return _typeCache.GetOrAdd<TMessage, IInitializerConvention<TMessage>>().TryGetHeaderInitializer<TInput, TProperty>(propertyInfo, out initializer);
    }

    /// <summary>
    /// Attempts to get headers initializer.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeadersInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class
    {
        return _typeCache.GetOrAdd<TMessage, IInitializerConvention<TMessage>>().TryGetHeadersInitializer<TInput, TProperty>(propertyInfo, out initializer);
    }
}
