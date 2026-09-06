using System.Diagnostics.CodeAnalysis;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Provides cached access to type converter data.</summary>
public interface ITypeConverterCache
{
    /// <summary>Attempts to get type converter.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <typeparam name="TInput">The input type.</typeparam>
    /// <param name="typeConverter">Receives the type converter produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    bool TryGetTypeConverter<TProperty, TInput>([NotNullWhen(true)] out ITypeConverter<TProperty, TInput>? typeConverter);
}
