using System;
using System.Reflection;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.MessageData.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Configures storage-policy application for one direct message-data property.</summary>
/// <typeparam name="TInput">The outgoing message type.</typeparam>
/// <typeparam name="TValue">The message-data value type.</typeparam>
internal sealed class PutMessageDataTransformConfiguration<TInput, TValue> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
{
    readonly PropertyInfo _property;
    readonly IMessageDataRepository _repository;
    readonly MessageDataPolicy _policy;

    /// <summary>Creates a property transformation bound to one repository and policy owner.</summary>
    /// <param name="repository">The repository used for external storage.</param>
    /// <param name="policy">The inline and retention policy.</param>
    /// <param name="property">The message-data property to transform.</param>
    public PutMessageDataTransformConfiguration(IMessageDataRepository repository, MessageDataPolicy policy, PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(property);

        _property = property;
        _repository = repository;
        _policy = policy;
    }

    /// <inheritdoc />
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        ArgumentNullException.ThrowIfNull(configurator);

        if (!MessageTypeCache<TInput>.IsValidMessageType)
            return;

        var inputPropertyProvider = new InputPropertyProvider<TInput, MessageData<TValue>>(_property);

        var provider = new PutMessageDataPropertyProvider<TInput, TValue>(inputPropertyProvider, _repository, _policy);

        configurator.Set(_property, provider);
    }
}
