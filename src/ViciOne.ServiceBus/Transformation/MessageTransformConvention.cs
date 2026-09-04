using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Conventions;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>
/// Provides a message transform convention implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageTransformConvention<TMessage> :
    IInitializerConvention<TMessage, TMessage>,
    IInitializerConvention<TMessage>,
    IInitializerConvention
    where TMessage : class
{
    readonly IDictionary<string, IPropertyInitializer<TMessage, TMessage>> _initializers;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public MessageTransformConvention()
    {
        _initializers = new Dictionary<string, IPropertyInitializer<TMessage, TMessage>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count => _initializers.Count;

    /// <summary>
    /// Attempts to get property initializer.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPropertyInitializer<T, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<T, TInput>? initializer)
        where T : class
        where TInput : class
    {
        if (this is IInitializerConvention<T, TInput> convention)
            return convention.TryGetPropertyInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
    }

    /// <summary>
    /// Attempts to get header initializer.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeaderInitializer<T, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<T, TInput>? initializer)
        where T : class
        where TInput : class
    {
        if (this is IInitializerConvention<T, TInput> convention)
            return convention.TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
    }

    /// <summary>
    /// Attempts to get headers initializer.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeadersInitializer<T, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<T, TInput>? initializer)
        where T : class
        where TInput : class
    {
        if (this is IInitializerConvention<T, TInput> convention)
            return convention.TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
    }

    /// <summary>
    /// Attempts to get property initializer.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPropertyInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TMessage>? initializer)
    {
        if (_initializers.TryGetValue(propertyInfo.Name, out initializer))
            return true;

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
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TMessage>? initializer)
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
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TMessage>? initializer)
    {
        initializer = default;
        return false;
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
        if (this is IInitializerConvention<TMessage, TInput> convention)
            return convention.TryGetPropertyInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
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
        if (this is IInitializerConvention<TMessage, TInput> convention)
            return convention.TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
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
        if (this is IInitializerConvention<TMessage, TInput> convention)
            return convention.TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="propertyName">The property name value.</param>
    /// <param name="initializer">The initializer value.</param>
    public void Add(string propertyName, IPropertyInitializer<TMessage, TMessage> initializer)
    {
        _initializers.Add(propertyName, initializer);
    }
}
