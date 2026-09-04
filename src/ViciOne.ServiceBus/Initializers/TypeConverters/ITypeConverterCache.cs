using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>
/// Defines the contract for type converter cache.
/// </summary>
public interface ITypeConverterCache
{
    /// <summary>
    /// Attempts to get type converter.
    /// </summary>
    /// <typeparam name="TProperty">The t property type.</typeparam>
    /// <typeparam name="TInput">The t input type.</typeparam>
    /// <param name="typeConverter">The type converter value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetTypeConverter<TProperty, TInput>([NotNullWhen(true)] out ITypeConverter<TProperty, TInput>? typeConverter);
}
