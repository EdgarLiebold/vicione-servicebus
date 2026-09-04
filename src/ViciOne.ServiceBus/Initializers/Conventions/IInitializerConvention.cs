using System.Reflection;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>
/// Defines the contract for initializer convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
/// <typeparam name="TInput">The t input type.</typeparam>
public interface IInitializerConvention<TMessage, TInput> :
    IMessageInputInitializerConvention<TMessage>
    where TMessage : class
    where TInput : class
{
    /// <summary>
    /// Attempts to get property initializer.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetPropertyInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer);
    /// <summary>
    /// Attempts to get header initializer.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetHeaderInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer);
    /// <summary>
    /// Attempts to get headers initializer.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetHeadersInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer);
}


/// <summary>
/// Defines the contract for initializer convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IInitializerConvention<TMessage> :
    IMessageInitializerConvention
    where TMessage : class
{
    /// <summary>
    /// Attempts to get property initializer.
    /// </summary>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetPropertyInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        where TInput : class;

    /// <summary>
    /// Attempts to get header initializer.
    /// </summary>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetHeaderInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TInput : class;

    /// <summary>
    /// Attempts to get headers initializer.
    /// </summary>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetHeadersInitializer<TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TInput : class;
}


/// <summary>
/// Defines the contract for initializer convention.
/// </summary>
public interface IInitializerConvention
{
    /// <summary>
    /// Attempts to get property initializer.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetPropertyInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class;

    /// <summary>
    /// Attempts to get header initializer.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetHeaderInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class;

    /// <summary>
    /// Attempts to get headers initializer.
    /// </summary>
    /// <typeparam name="TMessage">The t message type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="propertyInfo">The property info value.</param>
    /// <param name="initializer">The initializer value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetHeadersInitializer<TMessage, TInput, TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
        where TMessage : class
        where TInput : class;
}
