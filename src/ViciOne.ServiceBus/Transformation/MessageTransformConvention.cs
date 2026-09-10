using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.Conventions;

namespace ViciOne.ServiceBus.Transformation;

/// <summary>Maps explicitly configured properties to their transform initializers.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
internal sealed class MessageTransformConvention<TMessage> :
    IInitializerConvention<TMessage, TMessage>,
    IInitializerConvention<TMessage>,
    IInitializerConvention
    where TMessage : class
{
    readonly IDictionary<string, IPropertyInitializer<TMessage, TMessage>> _initializers;

    /// <summary>Creates an empty property-transform registry.</summary>
    public MessageTransformConvention()
    {
        _initializers = new Dictionary<string, IPropertyInitializer<TMessage, TMessage>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Gets the number of explicitly configured property transforms.</summary>
    public int Count => _initializers.Count;

    /// <summary>Attempts to get property initializer.</summary>
    /// <typeparam name="TTarget">The message type being initialized.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPropertyInitializer<TTarget, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TTarget, TInput>? initializer)
        where TTarget : class
        where TInput : class
    {
        if (this is IInitializerConvention<TTarget, TInput> convention)
            return convention.TryGetPropertyInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
    }

    /// <summary>Attempts to get header initializer.</summary>
    /// <typeparam name="TTarget">The message type being initialized.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeaderInitializer<TTarget, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TTarget, TInput>? initializer)
        where TTarget : class
        where TInput : class
    {
        if (this is IInitializerConvention<TTarget, TInput> convention)
            return convention.TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
    }

    /// <summary>Attempts to get headers initializer.</summary>
    /// <typeparam name="TTarget">The message type being initialized.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeadersInitializer<TTarget, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TTarget, TInput>? initializer)
        where TTarget : class
        where TInput : class
    {
        if (this is IInitializerConvention<TTarget, TInput> convention)
            return convention.TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
    }

    /// <summary>Attempts to get property initializer.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPropertyInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TMessage>? initializer)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);

        if (_initializers.TryGetValue(propertyInfo.Name, out initializer))
            return true;

        initializer = default;
        return false;
    }

    /// <summary>Attempts to get header initializer.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeaderInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TMessage>? initializer)
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
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TMessage>? initializer)
    {
        initializer = default;
        return false;
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
        if (this is IInitializerConvention<TMessage, TInput> convention)
            return convention.TryGetPropertyInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
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
        if (this is IInitializerConvention<TMessage, TInput> convention)
            return convention.TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
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
        if (this is IInitializerConvention<TMessage, TInput> convention)
            return convention.TryGetHeaderInitializer<TProperty>(propertyInfo, out initializer);

        initializer = default;
        return false;
    }

    /// <summary>Adds the initializer for one message property.</summary>
    /// <param name="propertyName">The case-insensitive property name.</param>
    /// <param name="initializer">The initializer that applies the configured transform.</param>
    public void Add(string propertyName, IPropertyInitializer<TMessage, TMessage> initializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentNullException.ThrowIfNull(initializer);
        _initializers.Add(propertyName, initializer);
    }
}
