using System.Reflection;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Maps one input type to writable message properties and outgoing send headers.</summary>
/// <typeparam name="TMessage">The message contract to populate.</typeparam>
/// <typeparam name="TInput">The input object that supplies values.</typeparam>
public interface IInitializerConvention<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    /// <summary>Attempts to create the mapping for a writable message property.</summary>
    /// <typeparam name="TProperty">The message property's declared type.</typeparam>
    /// <param name="propertyInfo">The writable message property.</param>
    /// <param name="initializer">The property mapping when this convention applies.</param>
    /// <returns><see langword="true" /> if this convention supplies the mapping; otherwise, <see langword="false" />.</returns>
    bool TryGetPropertyInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer);
    /// <summary>Attempts to map a standard send-header property from the input object.</summary>
    /// <typeparam name="TProperty">The send-header property's declared type.</typeparam>
    /// <param name="propertyInfo">The standard send-header property.</param>
    /// <param name="initializer">The header mapping when this convention applies.</param>
    /// <returns><see langword="true" /> if this convention supplies the mapping; otherwise, <see langword="false" />.</returns>
    bool TryGetHeaderInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer);
    /// <summary>Attempts to map an input property encoded as a custom send header.</summary>
    /// <typeparam name="TProperty">The input property's declared type.</typeparam>
    /// <param name="propertyInfo">The input property that may encode a custom header.</param>
    /// <param name="initializer">The custom-header mapping when this convention applies.</param>
    /// <returns><see langword="true" /> if this convention supplies the mapping; otherwise, <see langword="false" />.</returns>
    bool TryGetHeadersInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer);
}


/// <summary>Resolves initializer mappings for one message contract and any supported input type.</summary>
/// <typeparam name="TMessage">The message contract to populate.</typeparam>
public interface IInitializerConvention<TMessage>
    where TMessage : class
{
    /// <summary>Attempts to create a writable message-property mapping for the selected input type.</summary>
    /// <typeparam name="TInput">The input object type.</typeparam>
    /// <typeparam name="TProperty">The message property's declared type.</typeparam>
    /// <param name="propertyInfo">The writable message property.</param>
    /// <param name="initializer">The property mapping when this convention applies.</param>
    /// <returns><see langword="true" /> if this convention supplies the mapping; otherwise, <see langword="false" />.</returns>
    bool TryGetPropertyInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        where TInput : class;

    /// <summary>Attempts to map a standard send-header property for the selected input type.</summary>
    /// <typeparam name="TInput">The input object type.</typeparam>
    /// <typeparam name="TProperty">The send-header property's declared type.</typeparam>
    /// <param name="propertyInfo">The standard send-header property.</param>
    /// <param name="initializer">The header mapping when this convention applies.</param>
    /// <returns><see langword="true" /> if this convention supplies the mapping; otherwise, <see langword="false" />.</returns>
    bool TryGetHeaderInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TInput : class;

    /// <summary>Attempts to map an encoded custom-header property for the selected input type.</summary>
    /// <typeparam name="TInput">The input object type.</typeparam>
    /// <typeparam name="TProperty">The input property's declared type.</typeparam>
    /// <param name="propertyInfo">The input property that may encode a custom header.</param>
    /// <param name="initializer">The custom-header mapping when this convention applies.</param>
    /// <returns><see langword="true" /> if this convention supplies the mapping; otherwise, <see langword="false" />.</returns>
    bool TryGetHeadersInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TInput : class;
}


/// <summary>Resolves initializer mappings across message and input contract types.</summary>
public interface IInitializerConvention
{
    /// <summary>Attempts to create a writable message-property mapping.</summary>
    /// <typeparam name="TMessage">The message contract to populate.</typeparam>
    /// <typeparam name="TInput">The input object type.</typeparam>
    /// <typeparam name="TProperty">The message property's declared type.</typeparam>
    /// <param name="propertyInfo">The writable message property.</param>
    /// <param name="initializer">The property mapping when this convention applies.</param>
    /// <returns><see langword="true" /> if this convention supplies the mapping; otherwise, <see langword="false" />.</returns>
    bool TryGetPropertyInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class;

    /// <summary>Attempts to map a standard send-header property.</summary>
    /// <typeparam name="TMessage">The message contract to populate.</typeparam>
    /// <typeparam name="TInput">The input object type.</typeparam>
    /// <typeparam name="TProperty">The send-header property's declared type.</typeparam>
    /// <param name="propertyInfo">The standard send-header property.</param>
    /// <param name="initializer">The header mapping when this convention applies.</param>
    /// <returns><see langword="true" /> if this convention supplies the mapping; otherwise, <see langword="false" />.</returns>
    bool TryGetHeaderInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class;

    /// <summary>Attempts to map an encoded custom-header property.</summary>
    /// <typeparam name="TMessage">The message contract to populate.</typeparam>
    /// <typeparam name="TInput">The input object type.</typeparam>
    /// <typeparam name="TProperty">The input property's declared type.</typeparam>
    /// <param name="propertyInfo">The input property that may encode a custom header.</param>
    /// <param name="initializer">The custom-header mapping when this convention applies.</param>
    /// <returns><see langword="true" /> if this convention supplies the mapping; otherwise, <see langword="false" />.</returns>
    bool TryGetHeadersInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class;
}
