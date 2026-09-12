using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Converts a runtime-typed object property to the requested reference type.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
internal sealed class ObjectPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
    where TProperty : class
{
    readonly ConcurrentDictionary<Type, Converter> _converters;
    readonly IPropertyProviderFactory<TInput> _factory;
    readonly IPropertyProvider<TInput, object> _provider;

    /// <summary>Creates a provider that selects a converter from each source value's runtime type.</summary>
    /// <param name="factory">The factory that resolves runtime conversions.</param>
    /// <param name="provider">The provider that resolves the object-valued source.</param>
    public ObjectPropertyProvider(IPropertyProviderFactory<TInput> factory, IPropertyProvider<TInput, object> provider)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        _converters = new ConcurrentDictionary<Type, Converter>();
    }

    /// <summary>Resolves the object and applies the converter selected for its runtime type.</summary>
    /// <typeparam name="T">The message contract being initialized.</typeparam>
    /// <param name="context">The message and input object used for value resolution.</param>
    /// <param name="cancellationToken">The token that cancels source resolution and conversion.</param>
    /// <returns>A task containing the converted value, or <see langword="null" /> for a null source.</returns>
    public async Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        Task<object?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The object property provider returned null.");
        var propertyValue = await propertyTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (propertyValue == null)
            return null;

        var converter = _converters.GetOrAdd(propertyValue.GetType(), CreateConverter);
        Task<TProperty?> conversionTask = converter.ConvertAsync(context, propertyValue, cancellationToken)
            ?? throw new InvalidOperationException("The runtime property converter returned null.");
        return await conversionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    Converter CreateConverter(Type type)
    {
        Type converterType = typeof(ObjectConverter<>).MakeGenericType(typeof(TInput), typeof(TProperty), type);

        return (Converter)(Activator.CreateInstance(converterType, _factory)
            ?? throw new InvalidOperationException($"The runtime property converter '{converterType}' could not be activated."));
    }


    interface Converter
    {
        Task<TProperty?> ConvertAsync<T>(InitializeContext<T, TInput> context, object propertyValue,
            CancellationToken cancellationToken)
            where T : class;
    }


    sealed class ObjectConverter<TObject> :
        Converter
    {
        readonly IPropertyConverter<TProperty, TObject>? _converter;

        public ObjectConverter(IPropertyProviderFactory<TInput> factory)
        {
            ArgumentNullException.ThrowIfNull(factory);
            factory.TryGetPropertyConverter(out _converter);
        }

        public Task<TProperty?> ConvertAsync<T>(InitializeContext<T, TInput> context, object propertyValue,
            CancellationToken cancellationToken)
            where T : class
        {
            return _converter == null
                ? TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken)
                : _converter.ConvertAsync(context, (TObject)propertyValue, cancellationToken);
        }
    }
}
