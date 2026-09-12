using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.ServiceBus.Initializers.HeaderInitializers;
using ViciOne.ServiceBus.Initializers.PropertyInitializers;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Maps message properties and encoded headers from a property-bearing input object.</summary>
internal sealed class DefaultInitializerConvention<TMessage, TInput> :
    IInitializerConvention<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IReadOnlyDictionary<string, PropertyInfo> _inputProperties;
    readonly IPropertyProviderFactory<TInput> _providerFactory;

    public DefaultInitializerConvention()
    {
        _inputProperties = MessageTypeCache<TInput>.Properties.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _providerFactory = new PropertyProviderFactory<TInput>();
    }

    public bool TryGetPropertyInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IPropertyInitializer<TMessage, TInput>? initializer)
    {
        var propertyName = propertyInfo?.Name ?? throw new ArgumentNullException(nameof(propertyInfo));

        if (_inputProperties.TryGetValue(propertyName, out var inputPropertyInfo))
        {
            var propertyType = typeof(TProperty);
            var inputPropertyType = inputPropertyInfo.PropertyType;

            // Identical source and destination types require no conversion.
            if (inputPropertyType == propertyType)
            {
                initializer = new CopyPropertyInitializer<TMessage, TInput, TProperty>(propertyInfo, inputPropertyInfo);
                return true;
            }

            // An object destination preserves the runtime source type.
            if (propertyType == typeof(object))
            {
                if (inputPropertyType.TryGetTaskResultType(out var taskType))
                {
                    var type = typeof(CopyAsyncObjectPropertyInitializer<,,>).MakeGenericType(typeof(TMessage), typeof(TInput), taskType);
                    initializer = (IPropertyInitializer<TMessage, TInput>)(Activator.CreateInstance(type, propertyInfo, inputPropertyInfo) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
                }
                else
                {
                    var type = typeof(CopyObjectPropertyInitializer<,,>).MakeGenericType(typeof(TMessage), typeof(TInput), inputPropertyType);
                    initializer = (IPropertyInitializer<TMessage, TInput>)(Activator.CreateInstance(type, propertyInfo, inputPropertyInfo) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
                }

                return true;
            }

            if (_providerFactory.TryGetPropertyProvider(inputPropertyInfo, out IPropertyProvider<TInput, TProperty>? provider))
            {
                initializer = new ProviderPropertyInitializer<TMessage, TInput, TProperty>(provider, propertyInfo);
                return true;
            }
        }

        initializer = null;
        return false;
    }

    public bool TryGetHeaderInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
    {
        var propertyName = propertyInfo?.Name ?? throw new ArgumentNullException(nameof(propertyInfo));

        // Standard send headers are sourced from input properties prefixed with two underscores.
        var inputPropertyName = string.Concat("__", propertyName);

        if (_inputProperties.TryGetValue(inputPropertyName, out var inputPropertyInfo))
        {
            var propertyType = typeof(TProperty);
            var inputPropertyType = inputPropertyInfo.PropertyType;

            // Identical source and destination types require no conversion.
            if (inputPropertyType == propertyType)
            {
                initializer = new CopyHeaderInitializer<TMessage, TInput, TProperty>(propertyInfo, inputPropertyInfo);
                return true;
            }

            if (_providerFactory.TryGetPropertyProvider(inputPropertyInfo, out IPropertyProvider<TInput, TProperty>? provider))
            {
                initializer = new ProviderHeaderInitializer<TMessage, TInput, TProperty>(provider, propertyInfo);
                return true;
            }
        }

        initializer = default;
        return false;
    }

    public bool TryGetHeadersInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
    {
        var propertyName = propertyInfo?.Name ?? throw new ArgumentNullException(nameof(propertyInfo));

        if (propertyName.StartsWith("__Header_", StringComparison.Ordinal) && propertyName.Length > 9)
        {
            var headerName = propertyName[9..]
                .Replace("__", " ", StringComparison.Ordinal)
                .Replace("_", "-", StringComparison.Ordinal)
                .Replace(" ", "_", StringComparison.Ordinal);

            var inputPropertyType = propertyInfo.PropertyType;

            // A string property maps directly to the encoded header name.
            if (inputPropertyType == typeof(string))
            {
                initializer = new SetStringHeaderInitializer<TMessage, TInput>(headerName, propertyInfo);
                return true;
            }

            if (_providerFactory.TryGetPropertyProvider(propertyInfo, out IPropertyProvider<TInput, TProperty>? provider))
            {
                var type = typeof(SetHeaderInitializer<,,>).MakeGenericType(typeof(TMessage), typeof(TInput), inputPropertyType);
                initializer = (IHeaderInitializer<TMessage, TInput>)(Activator.CreateInstance(type, headerName, provider) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
                return true;
            }
        }

        initializer = default;
        return false;
    }
}


/// <summary>Dispatches the default mapping convention by input type for one message contract.</summary>
internal sealed class DefaultInitializerConvention<TMessage> :
    InitializerConvention<TMessage>
    where TMessage : class
{
    public DefaultInitializerConvention(IInitializerConvention convention)
        : base(new CacheFactory(), convention)
    {
    }


    sealed class CacheFactory :
        IConventionTypeCacheFactory
    {
        object IConventionTypeCacheFactory.Create<T>(IInitializerConvention convention)
        {
            return new DefaultInitializerConvention<TMessage, T>();
        }
    }
}


/// <summary>Dispatches the default mapping convention by message and input contract type.</summary>
internal sealed class DefaultInitializerConvention :
    InitializerConvention
{
    public DefaultInitializerConvention()
        : base(new CacheFactory())
    {
    }


    sealed class CacheFactory :
        IConventionTypeCacheFactory
    {
        object IConventionTypeCacheFactory.Create<T>(IInitializerConvention convention)
        {
            return new DefaultInitializerConvention<T>(convention);
        }
    }
}
