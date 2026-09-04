using System;
using System.Reflection;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.MessageData.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>
/// Provides a get message data transform configuration implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TValue">The t value type.</typeparam>
public class GetMessageDataTransformConfiguration<TInput, TValue> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
{
    readonly PropertyInfo _property;
    readonly IMessageDataRepository _repository;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="property">The property value.</param>
    public GetMessageDataTransformConfiguration(IMessageDataRepository repository, PropertyInfo property)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        _property = property;
        _repository = repository;
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        var inputPropertyProvider = new InputPropertyProvider<TInput, MessageData<TValue>>(_property);

        var provider = new GetMessageDataPropertyProvider<TInput, TValue>(inputPropertyProvider, _repository);

        configurator.Set(_property, provider);
    }
}
