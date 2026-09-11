using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Configures recursive storage for one nested object property.</summary>
/// <typeparam name="TInput">The containing message type.</typeparam>
/// <typeparam name="TProperty">The nested object type.</typeparam>
internal sealed class PutMessageDataObjectTransformConfiguration<TInput, TProperty> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
    where TProperty : class
{
    readonly PropertyInfo _property;
    readonly PutMessageDataTransformSpecification<TProperty> _transformConfigurator;

    /// <summary>Creates a recursive object transformation.</summary>
    /// <param name="repository">The repository used for external storage.</param>
    /// <param name="policy">The inline and retention policy.</param>
    /// <param name="knownTypes">The types already visited in the object graph.</param>
    /// <param name="property">The nested object property to transform.</param>
    public PutMessageDataObjectTransformConfiguration(IMessageDataRepository repository, MessageDataPolicy policy, IEnumerable<Type> knownTypes,
        PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(knownTypes);
        ArgumentNullException.ThrowIfNull(property);

        _property = property;

        _transformConfigurator = new PutMessageDataTransformSpecification<TProperty>(repository, policy, knownTypes);
    }

    /// <inheritdoc />
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (_transformConfigurator.TryGetConverter(out IPropertyConverter<TProperty, TProperty>? converter))
        {
            var inputPropertyProvider = new InputPropertyProvider<TInput, TProperty>(_property);

            var provider = new PropertyConverterPropertyProvider<TInput, TProperty, TProperty>(converter, inputPropertyProvider);

            configurator.Transform(_property, provider);
        }
    }
}
