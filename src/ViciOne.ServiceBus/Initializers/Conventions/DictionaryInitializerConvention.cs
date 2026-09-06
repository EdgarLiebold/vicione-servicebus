using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ViciOne.ServiceBus.Initializers.HeaderInitializers;
using ViciOne.ServiceBus.Initializers.PropertyInitializers;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Applies conventions for dictionary initializer.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public class DictionaryInitializerConvention<TMessage, TInput, TValue> :
    IInitializerConvention<TMessage, TInput>
    where TMessage : class
    where TInput : class, IDictionary<string, TValue>
{
    readonly IPropertyProviderFactory<TInput> _providerFactory;

    /// <summary>Initializes a new instance.</summary>
    public DictionaryInitializerConvention()
    {
        _providerFactory = new PropertyProviderFactory<TInput>();
    }

    /// <summary>Attempts to get property initializer.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetPropertyInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
    {
        var key = propertyInfo?.Name ?? throw new ArgumentNullException(nameof(propertyInfo));

        if (typeof(TValue) == typeof(TProperty))
        {
            initializer = new DictionaryCopyPropertyInitializer<TMessage, TInput, TValue>(propertyInfo, key);
            return true;
        }

        if (_providerFactory.TryGetPropertyConverter(out IPropertyConverter<TProperty, TValue>? converter))
        {
            var providerType = typeof(InputDictionaryPropertyProvider<,>).MakeGenericType(typeof(TInput), typeof(TValue));

            var provider = (IPropertyProvider<TInput, TValue>)(Activator.CreateInstance(providerType, key) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

            var convertProvider = new PropertyConverterPropertyProvider<TInput, TProperty, TValue>(converter, provider);

            initializer = new ProviderPropertyInitializer<TMessage, TInput, TProperty>(convertProvider, propertyInfo);
            return true;
        }

        if (typeof(TValue) == typeof(object))
        {
            var inputProviderType = typeof(InputDictionaryPropertyProvider<,>).MakeGenericType(typeof(TInput), typeof(TValue));

            var valueProvider = (IPropertyProvider<TInput, TValue>)(Activator.CreateInstance(inputProviderType, key) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

            var providerType = typeof(ObjectPropertyProvider<,>).MakeGenericType(typeof(TInput), typeof(TProperty));

            var provider = (IPropertyProvider<TInput, TProperty>)(Activator.CreateInstance(providerType, _providerFactory, valueProvider) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

            initializer = new ProviderPropertyInitializer<TMessage, TInput, TProperty>(provider, propertyInfo);
            return true;
        }

        initializer = null;
        return false;
    }

    /// <summary>Attempts to get header initializer.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeaderInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
    {
        var propertyName = propertyInfo?.Name ?? throw new ArgumentNullException(nameof(propertyInfo));

        // Header initializer properties use a double-underscore prefix.
        var key = new StringBuilder(propertyName.Length + 2).Append("__").Append(propertyName).ToString();

        if (typeof(TValue) == typeof(TProperty))
        {
            initializer = new DictionaryCopyHeaderInitializer<TMessage, TInput, TValue>(propertyInfo, key);
            return true;
        }

        if (_providerFactory.TryGetPropertyConverter(out IPropertyConverter<TProperty, TValue>? converter))
        {
            var providerType = typeof(InputDictionaryPropertyProvider<,>).MakeGenericType(typeof(TInput), typeof(TValue));

            var provider = (IPropertyProvider<TInput, TValue>)(Activator.CreateInstance(providerType, propertyName) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

            var convertProvider = new PropertyConverterPropertyProvider<TInput, TProperty, TValue>(converter, provider);

            initializer = new ProviderHeaderInitializer<TMessage, TInput, TProperty>(convertProvider, propertyInfo);
            return true;
        }

        initializer = default;
        return false;
    }

    /// <summary>Attempts to get headers initializer.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeadersInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
    {
        initializer = default;
        return false;
    }
}


/// <summary>Applies conventions for dictionary initializer.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class DictionaryInitializerConvention<TMessage> :
    InitializerConvention<TMessage>
    where TMessage : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="convention">The convention.</param>
    public DictionaryInitializerConvention(IInitializerConvention convention)
        : base(new CacheFactory(), convention)
    {
    }


    class CacheFactory :
        IConventionTypeCacheFactory<IMessageInputInitializerConvention<TMessage>>
    {
        IMessageInputInitializerConvention<TMessage> IConventionTypeCacheFactory<IMessageInputInitializerConvention<TMessage>>.Create<T>(
            IInitializerConvention convention)
        {
            if (typeof(T).TryGetSingleClosedGenericArguments(typeof(IDictionary<,>), out Type[] argumentTypes) && argumentTypes[0] == typeof(string))
            {
                var conventionType = typeof(DictionaryInitializerConvention<,,>).MakeGenericType(typeof(TMessage), typeof(T), argumentTypes[1]);

                return (IMessageInputInitializerConvention<TMessage>)(Activator.CreateInstance(conventionType) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
            }

            return new Unsupported<T>();
        }
    }
}


/// <summary>Applies conventions for dictionary initializer.</summary>
public class DictionaryInitializerConvention :
    InitializerConvention
{
    /// <summary>Initializes a new instance.</summary>
    public DictionaryInitializerConvention()
        : base(new CacheFactory())
    {
    }


    class CacheFactory :
        IConventionTypeCacheFactory<IMessageInitializerConvention>
    {
        IMessageInitializerConvention IConventionTypeCacheFactory<IMessageInitializerConvention>.Create<T>(IInitializerConvention convention)
        {
            return new DictionaryInitializerConvention<T>(convention);
        }
    }
}
