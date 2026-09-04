using System;
using System.Reflection;
using ViciOne.ServiceBus.Initializers.PropertyProviders;
using ViciOne.ServiceBus.MessageData.PropertyProviders;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>
/// Provides a put message data transform configuration implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
/// <typeparam name="TValue">The t value type.</typeparam>
public class PutMessageDataTransformConfiguration<TInput, TValue> :
    IMessageDataTransformConfiguration<TInput>
    where TInput : class
{
    readonly PropertyInfo _property;
    readonly IMessageDataRepository _repository;
    readonly MessageDataPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="repository">The repository value.</param>
    /// <param name="policy">The policy value.</param>
    /// <param name="property">The property value.</param>
    public PutMessageDataTransformConfiguration(IMessageDataRepository repository, MessageDataPolicy policy, PropertyInfo property)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));

        _property = property;
        _repository = repository;
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="configurator">The configurator value.</param>
    public void Apply(ITransformConfigurator<TInput> configurator)
    {
        if (!MessageTypeCache<TInput>.IsValidMessageType)
            return;

        var inputPropertyProvider = new InputPropertyProvider<TInput, MessageData<TValue>>(_property);

        var provider = new PutMessageDataPropertyProvider<TInput, TValue>(inputPropertyProvider, _repository, _policy);

        configurator.Set(_property, provider);
    }
}
