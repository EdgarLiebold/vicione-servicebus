using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Initializers.PropertyProviders;

/// <summary>Provides object property services.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class ObjectPropertyProvider<TInput, TProperty> :
    IPropertyProvider<TInput, TProperty>
    where TInput : class
    where TProperty : class
{
    readonly ConcurrentDictionary<Type, Converter> _converters;
    readonly IPropertyProviderFactory<TInput> _factory;
    readonly IPropertyProvider<TInput, object> _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="factory">The factory invoked by the operation.</param>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    public ObjectPropertyProvider(IPropertyProviderFactory<TInput> factory, IPropertyProvider<TInput, object> provider)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));

        _converters = new ConcurrentDictionary<Type, Converter>();
    }

    /// <summary>Gets property.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the requested value.</returns>
    public Task<TProperty?> GetPropertyAsync<T>(InitializeContext<T, TInput> context, CancellationToken cancellationToken = default)
        where T : class
    {
        Task<object?> propertyTask = _provider.GetPropertyAsync(context, cancellationToken: cancellationToken);
        if (propertyTask.Status == TaskStatus.RanToCompletion)
        {
            var propertyValue = propertyTask.Result;
            if (propertyValue == default)
                return TaskResults.DefaultAsync<TProperty>(cancellationToken: cancellationToken);

            var converter = _converters.GetOrAdd(propertyValue.GetType(), CreateConverter);

            return converter.ConvertAsync(context, propertyValue);
        }

        async Task<TProperty?> GetPropertyAsync()
        {
            var propertyValue = await propertyTask.ConfigureAwait(false);
            if (propertyValue == null)
                return null;

            var converter = _converters.GetOrAdd(propertyValue.GetType(), CreateConverter);

            return await converter.ConvertAsync(context, propertyValue).ConfigureAwait(false);
        }

        return GetPropertyAsync();
    }

    Converter CreateConverter(Type type)
    {
        return (Converter)(Activator.CreateInstance(typeof(ObjectConverter<>).MakeGenericType(typeof(TInput), typeof(TProperty), type), _factory) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
    }


    interface Converter
    {
        Task<TProperty?> ConvertAsync<T>(InitializeContext<T, TInput> context, object propertyValue)
            where T : class;
    }


    class ObjectConverter<TObject> :
        Converter
    {
        readonly IPropertyConverter<TProperty, TObject>? _converter;

        public ObjectConverter(IPropertyProviderFactory<TInput> factory)
        {
            factory.TryGetPropertyConverter(out _converter);
        }

        public Task<TProperty?> ConvertAsync<T>(InitializeContext<T, TInput> context, object propertyValue)
            where T : class
        {
            return _converter == null
                ? TaskResults.DefaultAsync<TProperty>()
                : _converter.ConvertAsync(context, (TObject)propertyValue);
        }
    }
}
