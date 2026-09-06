using System;
using System.Reflection;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.MessageData.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Stores and validates get message data transform configuration.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TValue">The value stored by the member.</typeparam>
public class GetMessageDataTransformConfiguration<TInput, TValue> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
{
    readonly PropertyInfo _property;
    readonly IMessageDataRepository _repository;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="property">The property.</param>
    public GetMessageDataTransformConfiguration(IMessageDataRepository repository, PropertyInfo property)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        _property = property;
        _repository = repository;
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="configurator">The configurator to update.</param>
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        var inputPropertyProvider = new InputPropertyProvider<TInput, MessageData<TValue>>(_property);

        var provider = new GetMessageDataPropertyProvider<TInput, TValue>(inputPropertyProvider, _repository);

        configurator.Set(_property, provider);
    }
}
