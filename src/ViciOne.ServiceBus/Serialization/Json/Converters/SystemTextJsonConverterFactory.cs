using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Batching.Contexts;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Internals.Reflection;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Serialization.Json.Converters;

/// <summary>Creates System.Text.Json converters for message contracts and supported dictionary shapes.</summary>
internal sealed class SystemTextJsonConverterFactory :
    JsonConverterFactory
{
    static SystemTextJsonConverterFactory()
    {
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(Fault<>), typeof(FaultEvent<>));
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(IMessageBatch<>), typeof(MessageBatch<>));
        JsonMessageTypeMappingRegistry.Register<Fault, FaultEvent>();
        JsonMessageTypeMappingRegistry.Register<ReceiveFault, ReceiveFaultEvent>();
        JsonMessageTypeMappingRegistry.Register<ExceptionInfo, FaultExceptionInfo>();
        JsonMessageTypeMappingRegistry.Register<HostInfo, BusHostInfo>();
        JsonMessageTypeMappingRegistry.Register<ScheduleMessage, ScheduleMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<ScheduleRecurringMessage, ScheduleRecurringMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<CancelScheduledMessage, CancelScheduledMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<CancelScheduledRecurringMessage, CancelScheduledRecurringMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<PauseScheduledRecurringMessage, PauseScheduledRecurringMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<ResumeScheduledRecurringMessage, ResumeScheduledRecurringMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<MessageEnvelope, JsonMessageEnvelope>();
    }

    /// <summary>Determines whether a type has a registered mapping, supported interface shape, or supported dictionary shape.</summary>
    /// <param name="typeToConvert">The candidate type.</param>
    /// <returns><see langword="true" /> when the factory supports the type; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="typeToConvert" /> is <see langword="null" />.</exception>
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        if (TryGetMaterializableDictionaryTypes(typeToConvert, out _, out _))
            return true;

        if (JsonMessageTypeMappingRegistry.Contains(typeToConvert))
            return true;

        if (!typeToConvert.IsInterface)
            return false;

        return IsConvertibleInterfaceType(typeToConvert);
    }

    /// <summary>Creates the converter for a supported contract or dictionary shape.</summary>
    /// <param name="typeToConvert">The supported type.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converter for <paramref name="typeToConvert" />.</returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null" />.</exception>
    /// <exception cref="ViciOneServiceBusException"><paramref name="typeToConvert" /> is not supported.</exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);

        if (JsonMessageTypeMappingRegistry.TryCreateConverter(typeToConvert, out JsonConverter? mappedConverter))
            return mappedConverter;

        if (TryCreateDictionaryConverter(typeToConvert, out JsonConverter dictionaryConverter))
            return dictionaryConverter;

        if (IsConvertibleInterfaceType(typeToConvert))
        {
            return (JsonConverter)(Activator.CreateInstance(
                typeof(InterfaceJsonConverter<,>).MakeGenericType(typeToConvert, MessageImplementationCache.GetImplementationType(typeToConvert))) ?? throw new InvalidOperationException("The requested runtime type could not be activated."));
        }

        throw new ViciOneServiceBusException($"Unsupported type for json serialization {TypeCache.GetShortName(typeToConvert)}");
    }

    static bool TryCreateDictionaryConverter(Type typeToConvert, out JsonConverter converter)
    {
        converter = null!;
        if (!TryGetMaterializableDictionaryTypes(typeToConvert, out Type keyType, out Type valueType))
            return false;

        Type converterType;
        if (keyType == typeof(string) && valueType == typeof(object))
            converterType = typeof(CaseInsensitiveDictionaryStringObjectJsonConverter<>).MakeGenericType(typeToConvert);
        else if (keyType == typeof(string))
            converterType = typeof(CaseInsensitiveDictionaryJsonConverter<,>).MakeGenericType(typeToConvert, valueType);
        else
            converterType = typeof(UriDictionarySystemTextJsonConverter<,>).MakeGenericType(typeToConvert, valueType);

        converter = (JsonConverter)(Activator.CreateInstance(converterType)
            ?? throw new InvalidOperationException("The requested runtime type could not be activated."));
        return true;
    }

    static bool TryGetMaterializableDictionaryTypes(Type typeToConvert, out Type keyType, out Type valueType)
    {
        keyType = null!;
        valueType = null!;
        if (!typeToConvert.IsGenericType || typeToConvert.IsFSharpType())
            return false;

        if (!TryGetDictionaryElementTypes(typeToConvert, out Type[] elementTypes))
            return false;

        keyType = elementTypes[0];
        valueType = elementTypes[1];
        if (keyType != typeof(string) && keyType != typeof(Uri))
            return false;

        Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        return typeToConvert.IsAssignableFrom(dictionaryType);
    }

    static bool TryGetDictionaryElementTypes(Type typeToConvert, out Type[] elementTypes)
    {
        if (typeToConvert.TryGetSingleClosedGenericArguments(typeof(IDictionary<,>), out elementTypes)
            || typeToConvert.TryGetSingleClosedGenericArguments(typeof(IReadOnlyDictionary<,>), out elementTypes)
            || typeToConvert.TryGetSingleClosedGenericArguments(typeof(Dictionary<,>), out elementTypes))
            return true;

        if (!typeToConvert.ClosesGenericType(typeof(IReadOnlyList<>))
            && typeToConvert.TryGetSingleClosedGenericArguments(typeof(IEnumerable<>), out Type[] enumerableTypes)
            && enumerableTypes[0].TryGetSingleClosedGenericArguments(typeof(KeyValuePair<,>), out elementTypes))
            return true;

        elementTypes = [];
        return false;
    }

    static bool IsConvertibleInterfaceType(Type typeToConvert)
    {
        if (!typeToConvert.IsInterfaceOrConcreteClass())
            return false;

        if (!MessageTypeCache.IsValidMessageType(typeToConvert))
            return false;

        if (typeToConvert.IsValueTypeOrObject())
            return false;

        foreach (var attribute in typeToConvert.GetCustomAttributes())
        {
            switch (attribute.GetType().Name)
            {
                case "JsonDerivedTypeAttribute":
                case "JsonPolymorphicAttribute":
                    return false;
            }
        }

        return true;
    }
}
