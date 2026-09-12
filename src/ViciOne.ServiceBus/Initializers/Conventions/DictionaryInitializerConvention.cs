using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers.HeaderInitializers;
using ViciOne.ServiceBus.Initializers.PropertyInitializers;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Maps message properties and standard send headers from a string-keyed input dictionary.</summary>
internal sealed class DictionaryInitializerConvention<TMessage, TInput, TValue> :
    IInitializerConvention<TMessage, TInput>
    where TMessage : class
    where TInput : class, IDictionary<string, TValue>
{
    readonly IPropertyProviderFactory<TInput> _providerFactory;

    public DictionaryInitializerConvention()
    {
        _providerFactory = new PropertyProviderFactory<TInput>();
    }

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

    public bool TryGetHeaderInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
    {
        var propertyName = propertyInfo?.Name ?? throw new ArgumentNullException(nameof(propertyInfo));

        // Standard send headers are sourced from dictionary keys prefixed with two underscores.
        var key = string.Concat("__", propertyName);

        if (typeof(TValue) == typeof(TProperty))
        {
            initializer = new DictionaryCopyHeaderInitializer<TMessage, TInput, TValue>(propertyInfo, key);
            return true;
        }

        if (_providerFactory.TryGetPropertyConverter(out IPropertyConverter<TProperty, TValue>? converter))
        {
            var providerType = typeof(InputDictionaryPropertyProvider<,>).MakeGenericType(typeof(TInput), typeof(TValue));

            var provider = (IPropertyProvider<TInput, TValue>)(Activator.CreateInstance(providerType, key) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

            var convertProvider = new PropertyConverterPropertyProvider<TInput, TProperty, TValue>(converter, provider);

            initializer = new ProviderHeaderInitializer<TMessage, TInput, TProperty>(convertProvider, propertyInfo);
            return true;
        }

        initializer = default;
        return false;
    }

    public bool TryGetHeadersInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
    {
        initializer = default;
        return false;
    }
}


/// <summary>Dispatches dictionary mappings by input type for one message contract.</summary>
internal sealed class DictionaryInitializerConvention<TMessage> :
    InitializerConvention<TMessage>
    where TMessage : class
{
    public DictionaryInitializerConvention(IInitializerConvention convention)
        : base(new CacheFactory(), convention)
    {
    }


    sealed class CacheFactory :
        IConventionTypeCacheFactory
    {
        object IConventionTypeCacheFactory.Create<T>(IInitializerConvention convention)
        {
            if (typeof(T).TryGetSingleClosedGenericArguments(typeof(IDictionary<,>), out Type[] argumentTypes) && argumentTypes[0] == typeof(string))
            {
                var conventionType = typeof(DictionaryInitializerConvention<,,>).MakeGenericType(typeof(TMessage), typeof(T), argumentTypes[1]);

                return Activator.CreateInstance(conventionType)
                    ?? throw new InvalidOperationException($"The dictionary convention '{conventionType}' could not be activated.");
            }

            return new Unsupported<T>();
        }
    }
}


/// <summary>Dispatches dictionary mappings by message and input contract type.</summary>
internal sealed class DictionaryInitializerConvention :
    InitializerConvention
{
    public DictionaryInitializerConvention()
        : base(new CacheFactory())
    {
    }


    sealed class CacheFactory :
        IConventionTypeCacheFactory
    {
        object IConventionTypeCacheFactory.Create<T>(IInitializerConvention convention)
        {
            return new DictionaryInitializerConvention<T>(convention);
        }
    }
}
