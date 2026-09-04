using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ViciOne.ServiceBus.Initializers;

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

    bool TryGetPropertyConverter<T, TProperty>([NotNullWhen(true)] out IPropertyConverter<T, TProperty>? converter);
}
