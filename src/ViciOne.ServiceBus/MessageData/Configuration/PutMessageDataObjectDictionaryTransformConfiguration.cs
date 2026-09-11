using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Initializers.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Configures recursive storage for a supported dictionary property.</summary>
/// <typeparam name="TInput">The containing message type.</typeparam>
/// <typeparam name="TProperty">The dictionary property type.</typeparam>
/// <typeparam name="TKey">The dictionary key type.</typeparam>
/// <typeparam name="TValue">The nested value type.</typeparam>
internal sealed class PutMessageDataObjectDictionaryTransformConfiguration<TInput, TProperty, TKey, TValue> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
    where TKey : notnull
    where TValue : class
{
    readonly PropertyInfo _property;
    readonly PutMessageDataTransformSpecification<TValue> _transformConfigurator;

    /// <summary>Creates a recursive dictionary transformation.</summary>
    /// <param name="repository">The repository used for external storage.</param>
    /// <param name="policy">The inline and retention policy.</param>
    /// <param name="knownTypes">The types already visited in the object graph.</param>
    /// <param name="property">The dictionary property to transform.</param>
    public PutMessageDataObjectDictionaryTransformConfiguration(IMessageDataRepository repository, MessageDataPolicy policy,
        IEnumerable<Type> knownTypes, PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(knownTypes);
        ArgumentNullException.ThrowIfNull(property);

        _property = property;

        _transformConfigurator = new PutMessageDataTransformSpecification<TValue>(repository, policy, knownTypes);
    }

    /// <inheritdoc />
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (_transformConfigurator.TryGetConverter(out IPropertyConverter<TValue, TValue>? converter))
        {
            var inputPropertyProvider = new InputPropertyProvider<TInput, TProperty>(_property);

            var dictionaryConverter = new DictionaryPropertyConverter<TKey, TValue, TValue>(converter) as IPropertyConverter<TProperty, TProperty>
                ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Message Data Storage",
                    "unknown",
                    $"Dictionary property type '{TypeCache<TProperty>.ShortName}' is not supported",
                    "Use a Dictionary<TKey, TValue> property for recursive message-data storage"));

            var provider = new PropertyConverterPropertyProvider<TInput, TProperty, TProperty>(dictionaryConverter, inputPropertyProvider);

            configurator.Transform(_property, provider);
        }
    }
}
