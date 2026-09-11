using System;
using System.Collections.Generic;
using System.Reflection;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Initializers.PropertyConverters;
using ViciOne.ServiceBus.Initializers.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Configures recursive storage for an array or supported list property.</summary>
/// <typeparam name="TInput">The containing message type.</typeparam>
/// <typeparam name="TProperty">The collection property type.</typeparam>
/// <typeparam name="TElement">The nested element type.</typeparam>
internal sealed class PutMessageDataObjectArrayTransformConfiguration<TInput, TProperty, TElement> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
    where TElement : class
{
    readonly PropertyInfo _property;
    readonly PutMessageDataTransformSpecification<TElement> _transformConfigurator;

    /// <summary>Creates a recursive collection transformation.</summary>
    /// <param name="repository">The repository used for external storage.</param>
    /// <param name="policy">The inline and retention policy.</param>
    /// <param name="knownTypes">The types already visited in the object graph.</param>
    /// <param name="property">The collection property to transform.</param>
    public PutMessageDataObjectArrayTransformConfiguration(IMessageDataRepository repository, MessageDataPolicy policy, IEnumerable<Type> knownTypes,
        PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(knownTypes);
        ArgumentNullException.ThrowIfNull(property);

        _property = property;

        _transformConfigurator = new PutMessageDataTransformSpecification<TElement>(repository, policy, knownTypes);
    }

    /// <inheritdoc />
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (_transformConfigurator.TryGetConverter(out IPropertyConverter<TElement, TElement>? converter))
        {
            var inputPropertyProvider = new InputPropertyProvider<TInput, TProperty>(_property);

            IPropertyConverter<TProperty, TProperty> arrayConverter = (typeof(TProperty).IsArray
                ? new ArrayPropertyConverter<TElement, TElement>(converter) as IPropertyConverter<TProperty, TProperty>
                : new ListPropertyConverter<TElement, TElement>(converter) as IPropertyConverter<TProperty, TProperty>)
                ?? throw new ConfigurationException(global::ViciOne.ServiceBus.Providers.Configuration.ConfigurationMessages.Create(
                    "Message Data Storage",
                    "unknown",
                    $"Collection property type '{TypeCache<TProperty>.ShortName}' is not supported",
                    "Use an array or List<T> property for recursive message-data storage"));

            var provider = new PropertyConverterPropertyProvider<TInput, TProperty, TProperty>(arrayConverter, inputPropertyProvider);

            configurator.Transform(_property, provider);
        }
    }
}
