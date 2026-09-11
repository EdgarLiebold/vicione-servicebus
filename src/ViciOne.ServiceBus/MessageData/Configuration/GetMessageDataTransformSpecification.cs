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

/// <summary>Builds consume transformations that resolve every message-data property in a contract graph.</summary>
/// <typeparam name="TMessage">The root message contract type.</typeparam>
internal sealed class GetMessageDataTransformSpecification<TMessage> :
    TransformSpecification<TMessage>,
    IConsumeTransformSpecification<TMessage>,
    IExecuteTransformSpecification<TMessage>,
    ICompensateTransformSpecification<TMessage>
    where TMessage : class
{
    /// <summary>Discovers direct and nested message-data properties for one repository owner.</summary>
    /// <param name="repository">The repository that owns external references.</param>
    /// <param name="knownTypes">The optional set of object-graph types already visited.</param>
    public GetMessageDataTransformSpecification(IMessageDataRepository repository, IEnumerable<Type>? knownTypes = null)
    {
        ArgumentNullException.ThrowIfNull(repository);

        Replace = true;

        var types = new HashSet<Type>(knownTypes ?? []);
        if (types.Remove(null!))
            throw new ArgumentException("Known message-data types cannot contain null.", nameof(knownTypes));
        types.Add(typeof(TMessage));

        AddMessageDataProperties(repository, types);
    }

    void IPipeSpecification<CompensateContext<TMessage>>.Apply(IPipeBuilder<CompensateContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (Count > 0)
        {
            IMessageInitializer<TMessage> initializer = Build();

            builder.AddFilter(new TransformFilter<TMessage>(initializer));
        }
    }

    void IPipeSpecification<ConsumeContext<TMessage>>.Apply(IPipeBuilder<ConsumeContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (Count > 0)
        {
            IMessageInitializer<TMessage> initializer = Build();

            builder.AddFilter(new TransformFilter<TMessage>(initializer));
        }
    }

    void IPipeSpecification<ExecuteContext<TMessage>>.Apply(IPipeBuilder<ExecuteContext<TMessage>> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (Count > 0)
        {
            IMessageInitializer<TMessage> initializer = Build();

            builder.AddFilter(new TransformFilter<TMessage>(initializer));
        }
    }

    /// <summary>Creates a consume topology when the contract graph contains message-data properties.</summary>
    /// <param name="topology">Receives the configured topology when one is required.</param>
    /// <returns><see langword="true" /> when a transformation is required; otherwise, <see langword="false" />.</returns>
    public bool TryGetConsumeTopology([NotNullWhen(true)] out IMessageConsumeTopology<TMessage>? topology)
    {
        if (Count > 0)
        {
            IMessageInitializer<TMessage> initializer = Build();

            topology = new MessageDataMessageConsumeTopology<TMessage>(initializer);
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

    void AddMessageDataProperties(IMessageDataRepository repository, ICollection<Type> knownTypes)
    {
        foreach (var propertyInfo in MessageTypeCache<TMessage>.Properties)
        {
            var propertyType = propertyInfo.PropertyType;

            void ConfigureDictionary(Type keyType, Type valueType)
            {
                if (!IsUnknownObjectType(knownTypes, valueType))
                    return;

                var providerType = typeof(GetMessageDataObjectDictionaryTransformConfiguration<,,,>)
                    .MakeGenericType(typeof(TMessage), propertyType, keyType, valueType);
                var configuration = (IMessageDataTransformConfiguration<TMessage>)(Activator.CreateInstance(providerType, repository, knownTypes,
                    propertyInfo) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

                configuration.Apply(this);
            }

            void ConfigureArray(Type? elementType)
            {
                if (elementType == null || !IsUnknownObjectType(knownTypes, elementType))
                    return;

                var providerType = typeof(GetMessageDataObjectArrayTransformConfiguration<,,>)
                    .MakeGenericType(typeof(TMessage), propertyType, elementType);
                var configuration = (IMessageDataTransformConfiguration<TMessage>)(Activator.CreateInstance(providerType, repository, knownTypes,
                    propertyInfo) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

                configuration.Apply(this);
            }

            if (propertyType.TryGetSingleClosedGenericArguments(typeof(MessageData<>), out Type[] types))
            {
                var providerType = typeof(GetMessageDataTransformConfiguration<,>).MakeGenericType(typeof(TMessage), types[0]);
                var configuration = (IMessageDataTransformConfiguration<TMessage>)(Activator.CreateInstance(providerType, repository, propertyInfo) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));

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
                var providerType = typeof(GetMessageDataObjectTransformConfiguration<,>).MakeGenericType(typeof(TMessage), propertyType);
                var configuration = (IMessageDataTransformConfiguration<TMessage>)(Activator.CreateInstance(providerType, repository, knownTypes,
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
