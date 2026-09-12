using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ViciOne.ServiceBus.Initializers;

/// <summary>Resolves value providers and conversions for one input-object type.</summary>
internal interface IPropertyProviderFactory<TInput>
    where TInput : class
{
    /// <summary>Attempts to create a provider that reads and converts <paramref name="propertyInfo" /> to <typeparamref name="TResult" />.</summary>
    bool TryGetPropertyProvider<TResult>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyProvider<TInput, TResult>? provider);

    /// <summary>Attempts to resolve a conversion from <typeparamref name="TProperty" /> to <typeparamref name="T" />.</summary>
    bool TryGetPropertyConverter<T, TProperty>([NotNullWhen(true)] out IPropertyConverter<T, TProperty>? converter);
}
