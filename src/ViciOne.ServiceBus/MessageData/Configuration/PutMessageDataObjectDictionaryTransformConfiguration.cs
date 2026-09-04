using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Initializers.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>
/// Provides a put message data object dictionary transform configuration implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
/// <typeparam name="TKey">The t key type.</typeparam>
/// <typeparam name="TValue">The t value type.</typeparam>
public class PutMessageDataObjectDictionaryTransformConfiguration<TInput, TProperty, TKey, TValue> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
    where TKey : notnull
    where TValue : class
{
    readonly PropertyInfo _property;
    readonly PutMessageDataTransformSpecification<TValue> _transformConfigurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="policy">The policy value.</param>
    /// <param name="knownTypes">The known types value.</param>
    /// <param name="property">The property value.</param>
    public PutMessageDataObjectDictionaryTransformConfiguration(IMessageDataRepository repository, MessageDataPolicy policy,
        IEnumerable<Type> knownTypes, PropertyInfo property)
    {
        _property = property;

        _transformConfigurator = new PutMessageDataTransformSpecification<TValue>(repository, policy, knownTypes);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        if (_transformConfigurator.TryGetConverter(out IPropertyConverter<TValue, TValue>? converter))
        {
            var inputPropertyProvider = new InputPropertyProvider<TInput, TProperty>(_property);

            var dictionaryConverter = new DictionaryPropertyConverter<TKey, TValue, TValue>(converter) as IPropertyConverter<TProperty, TProperty>;

            var provider = new PropertyConverterPropertyProvider<TInput, TProperty, TProperty>(dictionaryConverter, inputPropertyProvider);

            configurator.Transform(_property, provider);
        }
    }
}
