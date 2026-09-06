using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Initializers.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Stores and validates put message data object dictionary transform configuration.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <typeparam name="TKey">The key used for lookup.</typeparam>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public class PutMessageDataObjectDictionaryTransformConfiguration<TInput, TProperty, TKey, TValue> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
    where TKey : notnull
    where TValue : class
{
    readonly PropertyInfo _property;
    readonly PutMessageDataTransformSpecification<TValue> _transformConfigurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="policy">The policy.</param>
    /// <param name="knownTypes">The known types.</param>
    /// <param name="property">The property.</param>
    public PutMessageDataObjectDictionaryTransformConfiguration(IMessageDataRepository repository, MessageDataPolicy policy,
        IEnumerable<Type> knownTypes, PropertyInfo property)
    {
        _property = property;

        _transformConfigurator = new PutMessageDataTransformSpecification<TValue>(repository, policy, knownTypes);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="configurator">The configurator to update.</param>
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
