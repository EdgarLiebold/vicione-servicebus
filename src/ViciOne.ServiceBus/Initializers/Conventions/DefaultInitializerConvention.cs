using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ViciOne.ServiceBus.Initializers.HeaderInitializers;
using ViciOne.ServiceBus.Initializers.PropertyInitializers;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.Conventions;

/// <summary>Applies conventions for default initializer.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
/// <typeparam name="TInput">The input type.</typeparam>
public class DefaultInitializerConvention<TMessage, TInput> :
    IInitializerConvention<TMessage, TInput>
    where TMessage : class
    where TInput : class
{
    readonly IReadOnlyDictionary<string, PropertyInfo> _inputProperties;
    readonly IPropertyProviderFactory<TInput> _providerFactory;

    /// <summary>Initializes a new instance.</summary>
    public DefaultInitializerConvention()
    {
        _inputProperties = MessageTypeCache<TInput>.Properties.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
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

    /// <summary>Attempts to get header initializer.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeaderInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
    {
        var propertyName = propertyInfo?.Name ?? throw new ArgumentNullException(nameof(propertyInfo));

        // A double underscore identifies a header initializer property.
        var inputPropertyName = new StringBuilder(propertyName.Length + 2).Append("__").Append(propertyName).ToString();

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

    /// <summary>Attempts to get headers initializer.</summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="propertyInfo">The property info.</param>
    /// <param name="initializer">Receives the initializer produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetHeadersInitializer<TProperty>(PropertyInfo propertyInfo,
        [NotNullWhen(true)] out IHeaderInitializer<TMessage, TInput>? initializer)
    {
        var propertyName = propertyInfo?.Name ?? throw new ArgumentNullException(nameof(propertyInfo));

        if (propertyName.StartsWith("__Header_") && propertyName.Length > 9)
        {
            var headerName = propertyName.Substring(9).Replace("__", " ").Replace("_", "-").Replace(" ", "_");

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


/// <summary>Applies conventions for default initializer.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class DefaultInitializerConvention<TMessage> :
    InitializerConvention<TMessage>
    where TMessage : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="convention">The convention.</param>
    public DefaultInitializerConvention(IInitializerConvention convention)
        : base(new CacheFactory(), convention)
    {
    }


    class CacheFactory :
        IConventionTypeCacheFactory<IMessageInputInitializerConvention<TMessage>>
    {
        IMessageInputInitializerConvention<TMessage> IConventionTypeCacheFactory<IMessageInputInitializerConvention<TMessage>>.Create<T>(
            IInitializerConvention convention)
        {
            return new DefaultInitializerConvention<TMessage, T>();
        }
    }
}


/// <summary>Applies conventions for default initializer.</summary>
public class DefaultInitializerConvention :
    InitializerConvention
{
    /// <summary>Initializes a new instance.</summary>
    public DefaultInitializerConvention()
        : base(new CacheFactory())
    {
    }


    class CacheFactory :
        IConventionTypeCacheFactory<IMessageInitializerConvention>
    {
        IMessageInitializerConvention IConventionTypeCacheFactory<IMessageInitializerConvention>.Create<T>(IInitializerConvention convention)
        {
            return new DefaultInitializerConvention<T>(convention);
        }
    }
}
