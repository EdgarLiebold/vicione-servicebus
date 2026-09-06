using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Initializers.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Stores and validates get message data object array transform configuration.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TProperty">The property type.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
public class GetMessageDataObjectArrayTransformConfiguration<TInput, TProperty, TElement> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
    where TElement : class
{
    readonly PropertyInfo _property;
    readonly GetMessageDataTransformSpecification<TElement> _transformConfigurator;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="knownTypes">The known types.</param>
    /// <param name="property">The property.</param>
    public GetMessageDataObjectArrayTransformConfiguration(IMessageDataRepository repository, IEnumerable<Type> knownTypes, PropertyInfo property)
    {
        _property = property;

        _transformConfigurator = new GetMessageDataTransformSpecification<TElement>(repository, knownTypes);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        if (_transformConfigurator.TryGetConverter(out IPropertyConverter<TElement, TElement>? converter))
        {
            var inputPropertyProvider = new InputPropertyProvider<TInput, TProperty>(_property);

            IPropertyConverter<TProperty, TProperty>? arrayConverter = typeof(TProperty).IsArray
                ? new ArrayPropertyConverter<TElement, TElement>(converter) as IPropertyConverter<TProperty, TProperty>
                : new ListPropertyConverter<TElement, TElement>(converter) as IPropertyConverter<TProperty, TProperty>;

            var provider = new PropertyConverterPropertyProvider<TInput, TProperty, TProperty>(arrayConverter, inputPropertyProvider);

            configurator.Set(_property, provider);
        }
    }
}
