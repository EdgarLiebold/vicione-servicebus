using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>
/// Defines the contract for property provider factory.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
public interface IPropertyProviderFactory<TInput>
    where TInput : class
{
    /// <summary>
    /// Return the factory to create a property provider for the specified type <typeparamref name="TResult" /> using the
    /// <paramref name="propertyInfo" /> as the source.
    /// </summary>
    /// <param name="propertyInfo">The input property</param>
    /// <param name="provider"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    bool TryGetPropertyProvider<TResult>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyProvider<TInput, TResult>? provider);

    /// <summary>
    /// Attempts to get property converter.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <param name="converter">The converter value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetPropertyConverter<T, TProperty>([NotNullWhen(true)] out IPropertyConverter<T, TProperty>? converter);
}
