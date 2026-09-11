using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.MessageData.Conventions;
using ViciOne.ServiceBus.MessageData.Internals;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transformation;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Builds send transformations that apply message-data storage policy across a contract graph.</summary>
/// <typeparam name="TMessage">The root message contract type.</typeparam>
internal sealed class PutMessageDataTransformSpecification<TMessage> :
    TransformSpecification<TMessage>,
    ISendTransformSpecification<TMessage>
    where TMessage : class
{
    /// <summary>Discovers direct and nested message-data properties for one repository and policy owner.</summary>
    /// <param name="repository">The repository used for external storage.</param>
    /// <param name="policy">The inline and retention policy.</param>
    /// <param name="knownTypes">The optional set of object-graph types already visited.</param>
    public PutMessageDataTransformSpecification(IMessageDataRepository repository, MessageDataPolicy policy, IEnumerable<Type>? knownTypes = null)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(policy);

        Replace = true;

        var types = new HashSet<Type>(knownTypes ?? []);
        if (types.Remove(null!))
            throw new ArgumentException("Known message-data types cannot contain null.", nameof(knownTypes));
        types.Add(typeof(TMessage));

        AddMessageDataProperties(repository, policy, types);
    }

    void IPipeSpecification<SendContext<TMessage>>.Apply(IPipeBuilder<SendContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (Count > 0)
        {
            IMessageInitializer<TMessage> initializer = Build();

            builder.AddFilter(new TransformFilter<TMessage>(initializer));
        }
    }

    /// <summary>Creates a send topology when the contract graph contains message-data properties.</summary>
    /// <param name="topology">Receives the configured topology when one is required.</param>
    /// <returns><see langword="true" /> when a transformation is required; otherwise, <see langword="false" />.</returns>
    public bool TryGetSendTopology([NotNullWhen(true)] out IMessageSendTopology<TMessage>? topology)
    {
        if (Count > 0)
        {
            IMessageInitializer<TMessage> initializer = Build();

            topology = new MessageDataMessageSendTopology<TMessage>(initializer);
            return true;
        }

        topology = default;
        return false;
    }

    /// <summary>Creates an object-graph converter when the contract contains message-data properties.</summary>
    /// <param name="converter">Receives the configured converter when one is required.</param>
    /// <returns><see langword="true" /> when a transformation is required; otherwise, <see langword="false" />.</returns>
    public bool TryGetConverter([NotNullWhen(true)] out IPropertyConverter<TMessage, TMessage>? converter)
    {
        if (Count > 0)
        {
            converter = new TransformPropertyConverter<TMessage>(Build());
            return true;
        }

        converter = default;
        return false;
    }

    void AddMessageDataProperties(IMessageDataRepository repository, MessageDataPolicy policy, ICollection<Type> knownTypes)
    {
        foreach (var propertyInfo in MessageTypeCache<TMessage>.Properties)
        {
            var propertyType = propertyInfo.PropertyType;

            void ConfigureDictionary(Type keyType, Type valueType)
            {
                if (!IsUnknownObjectType(knownTypes, valueType))
                    return;

                var providerType = typeof(PutMessageDataObjectDictionaryTransformConfiguration<,,,>)
                    .MakeGenericType(typeof(TMessage), propertyType, keyType, valueType);
                var configuration = (IMessageDataTransformConfiguration<TMessage>)(Activator.CreateInstance(providerType, repository, policy, knownTypes,
                    propertyInfo) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

                configuration.Apply(this);
            }

            void ConfigureArray(Type? elementType)
            {
                if (elementType == null || !IsUnknownObjectType(knownTypes, elementType))
                    return;

                var providerType = typeof(PutMessageDataObjectArrayTransformConfiguration<,,>)
                    .MakeGenericType(typeof(TMessage), propertyType, elementType);
                var configuration = (IMessageDataTransformConfiguration<TMessage>)(Activator.CreateInstance(providerType, repository, policy, knownTypes,
                    propertyInfo) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

                configuration.Apply(this);
            }

            if (propertyType.TryGetSingleClosedGenericArguments(typeof(MessageData<>), out Type[] types))
            {
                var providerType = typeof(PutMessageDataTransformConfiguration<,>).MakeGenericType(typeof(TMessage), types[0]);
                var configuration = (IMessageDataTransformConfiguration<TMessage>)(Activator.CreateInstance(providerType, repository, policy, propertyInfo) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

                configuration.Apply(this);
            }
            else if (propertyType.IsNullable(out _) || propertyType.IsValueTypeOrObject())
                continue;
            else if (propertyType.TryGetSingleClosedGenericArguments(typeof(IDictionary<,>), out types) || propertyType.TryGetSingleClosedGenericArguments(typeof(IReadOnlyDictionary<,>), out types))
                ConfigureDictionary(types[0], types[1]);
            else if (propertyType.IsArray)
                ConfigureArray(propertyType.GetElementType());
            else if (propertyType.TryGetSingleClosedGenericArguments(typeof(IEnumerable<>), out Type[] enumerableTypes))
            {
                if (enumerableTypes[0].TryGetSingleClosedGenericArguments(typeof(KeyValuePair<,>), out types))
                    ConfigureDictionary(types[0], types[1]);
                else
                    ConfigureArray(enumerableTypes[0]);
            }
            else if (IsUnknownObjectType(knownTypes, propertyType))
            {
                var providerType = typeof(PutMessageDataObjectTransformConfiguration<,>).MakeGenericType(typeof(TMessage), propertyType);
                var configuration = (IMessageDataTransformConfiguration<TMessage>)(Activator.CreateInstance(providerType, repository, policy, knownTypes,
                    propertyInfo) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

                configuration.Apply(this);
            }
        }
    }

    static bool IsUnknownObjectType(ICollection<Type> knownTypes, Type propertyType)
    {
        return MessageDataTypeClassifier.IsSupported(propertyType) && !knownTypes.Contains(propertyType);
    }
}
