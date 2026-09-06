using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.MessageData.Conventions;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Transformation;

namespace ViciOne.ServiceBus.MessageData.Configuration;

/// <summary>Describes requirements for put message data transform.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class PutMessageDataTransformSpecification<TMessage> :
    TransformSpecification<TMessage>,
    ISendTransformSpecification<TMessage>
    where TMessage : class
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="repository">The repository.</param>
    /// <param name="policy">The policy.</param>
    /// <param name="knownTypes">The known types.</param>
    public PutMessageDataTransformSpecification(IMessageDataRepository repository, MessageDataPolicy policy, IEnumerable<Type>? knownTypes = null)
    {
        if (repository == null)
            throw new ArgumentNullException(nameof(repository));
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        Replace = true;

        var types = new HashSet<Type>(knownTypes ?? Enumerable.Empty<Type>()) { typeof(TMessage) };

        AddMessageDataProperties(repository, policy, types);
    }

    void IPipeSpecification<SendContext<TMessage>>.Apply(IPipeBuilder<SendContext<TMessage>> builder)
    {
        if (Count > 0)
        {
            IMessageInitializer<TMessage> initializer = Build();

            builder.AddFilter(new TransformFilter<TMessage>(initializer));
        }
    }

    /// <summary>Attempts to get send topology.</summary>
    /// <param name="topology">Receives the topology produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>Attempts to get converter.</summary>
    /// <param name="converter">Receives the converter produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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
            else if (propertyType.IsNullable(out _))
            {
            }
            else if (propertyType.IsValueTypeOrObject())
            {
            }
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
        return propertyType.IsInterfaceOrConcreteClass() && MessageTypeCache.IsValidMessageType(propertyType) && !propertyType.IsValueTypeOrObject()
            && !knownTypes.Contains(propertyType);
    }
}
