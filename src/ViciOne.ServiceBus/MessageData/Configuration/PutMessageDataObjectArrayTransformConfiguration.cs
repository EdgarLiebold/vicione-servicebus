using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Initializers.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>
/// Provides a put message data object array transform configuration implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TProperty">The t property type.</typeparam>
/// <typeparam name="TElement">The t element type.</typeparam>
public class PutMessageDataObjectArrayTransformConfiguration<TInput, TProperty, TElement> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
    where TElement : class
{
    readonly PropertyInfo _property;
    readonly PutMessageDataTransformSpecification<TElement> _transformConfigurator;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="policy">The policy value.</param>
    /// <param name="knownTypes">The known types value.</param>
    /// <param name="property">The property value.</param>
    public PutMessageDataObjectArrayTransformConfiguration(IMessageDataRepository repository, MessageDataPolicy policy, IEnumerable<Type> knownTypes,
        PropertyInfo property)
    {
        _property = property;

        _transformConfigurator = new PutMessageDataTransformSpecification<TElement>(repository, policy, knownTypes);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        if (_transformConfigurator.TryGetConverter(out IPropertyConverter<TElement, TElement>? converter))
        {
            var inputPropertyProvider = new InputPropertyProvider<TInput, TProperty>(_property);

            IPropertyConverter<TProperty, TProperty>? arrayConverter = typeof(TProperty).IsArray
                ? new ArrayPropertyConverter<TElement, TElement>(converter) as IPropertyConverter<TProperty, TProperty>
                : new ListPropertyConverter<TElement, TElement>(converter) as IPropertyConverter<TProperty, TProperty>;

            var provider = new PropertyConverterPropertyProvider<TInput, TProperty, TProperty>(arrayConverter, inputPropertyProvider);

            configurator.Transform(_property, provider);
        }
    }
}
