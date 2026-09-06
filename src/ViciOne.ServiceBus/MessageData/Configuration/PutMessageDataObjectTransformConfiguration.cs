using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Stores and validates put message data object transform configuration.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
public class PutMessageDataObjectTransformConfiguration<TInput, TProperty> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
    where TProperty : class
{
    readonly PropertyInfo _property;
    readonly PutMessageDataTransformSpecification<TProperty> _transformConfigurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="policy">The policy.</param>
    /// <param name="knownTypes">The known types.</param>
    /// <param name="property">The property.</param>
    public PutMessageDataObjectTransformConfiguration(IMessageDataRepository repository, MessageDataPolicy policy, IEnumerable<Type> knownTypes,
        PropertyInfo property)
    {
        _property = property;

        _transformConfigurator = new PutMessageDataTransformSpecification<TProperty>(repository, policy, knownTypes);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        if (_transformConfigurator.TryGetConverter(out IPropertyConverter<TProperty, TProperty>? converter))
        {
            var inputPropertyProvider = new InputPropertyProvider<TInput, TProperty>(_property);

            var provider = new PropertyConverterPropertyProvider<TInput, TProperty, TProperty>(converter, inputPropertyProvider);

            configurator.Transform(_property, provider);
        }
    }
}
