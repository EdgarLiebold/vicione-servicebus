using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Creates property provider instances.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
public interface IPropertyProviderFactory<TInput>
    where TInput : class
{
    /// <summary>
    /// Return the factory to create a property provider for the specified type <typeparamref name="TResult" /> using the
    /// <paramref name="propertyInfo" /> as the source.
    /// </summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="propertyInfo">The input property.</param>
    /// <param name="provider">Receives the provider produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetPropertyProvider<TResult>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyProvider<TInput, TResult>? provider);

    /// <summary>Attempts to get property converter.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="converter">Receives the converter produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetPropertyConverter<T, TProperty>([NotNullWhen(true)] out IPropertyConverter<T, TProperty>? converter);
}
