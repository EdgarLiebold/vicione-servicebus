using System;
using System.Reflection;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.MessageData.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Configures loading for one direct message-data property.</summary>
/// <typeparam name="TInput">The incoming message type.</typeparam>
/// <typeparam name="TValue">The message-data value type.</typeparam>
internal sealed class GetMessageDataTransformConfiguration<TInput, TValue> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
{
    readonly PropertyInfo _property;
    readonly IMessageDataRepository _repository;

    /// <summary>Creates a property transformation bound to one repository.</summary>
    /// <param name="repository">The repository that owns external references.</param>
    /// <param name="property">The message-data property to transform.</param>
    public GetMessageDataTransformConfiguration(IMessageDataRepository repository, PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(property);

        _property = property;
        _repository = repository;
    }

    /// <inheritdoc />
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        var inputPropertyProvider = new InputPropertyProvider<TInput, MessageData<TValue>>(_property);

        var provider = new GetMessageDataPropertyProvider<TInput, TValue>(inputPropertyProvider, _repository);

        configurator.Set(_property, provider);
    }
}
