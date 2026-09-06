using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Batching;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>Creates system text json converter instances.</summary>
public class SystemTextJsonConverterFactory :
    JsonConverterFactory
{
    static SystemTextJsonConverterFactory()
    {
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(Fault<>), typeof(FaultEvent<>));
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(Batch<>), typeof(MessageBatch<>));
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

    /// <summary>Determines whether the current value can convert.</summary>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert.IsGenericType)
        {
            if (typeToConvert.TryGetSingleClosedGenericArguments(typeof(IDictionary<,>), out Type[] elementTypes)
                || typeToConvert.TryGetSingleClosedGenericArguments(typeof(IReadOnlyDictionary<,>), out elementTypes)
                || typeToConvert.TryGetSingleClosedGenericArguments(typeof(Dictionary<,>), out elementTypes)
                || (typeToConvert.TryGetSingleClosedGenericArguments(typeof(IEnumerable<>), out Type[] enumerableType)
                    && enumerableType[0].TryGetSingleClosedGenericArguments(typeof(KeyValuePair<,>), out elementTypes)
                    && elementTypes[1] == typeof(object)
                    && !typeToConvert.ClosesGenericType(typeof(IReadOnlyList<>))))
            {
                var keyType = elementTypes[0];

                if (keyType != typeof(string) && keyType != typeof(Uri))
                    return false;

                if (typeToConvert.IsFSharpType())
                    return false;

                return true;
            }
        }

        if (!typeToConvert.IsInterface)
            return false;

        if (JsonMessageTypeMappingRegistry.Contains(typeToConvert))
            return true;

        if (IsConvertibleInterfaceType(typeToConvert))
            return true;

        return false;
    }

    /// <summary>Creates converter.</summary>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The created converter.</returns>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        if (JsonMessageTypeMappingRegistry.TryCreateConverter(typeToConvert, out JsonConverter? mappedConverter))
            return mappedConverter;

        if (typeToConvert.IsGenericType)
        {
            if (!typeToConvert.IsFSharpType())
            {
                if (typeToConvert == typeof(IDictionary<string, object>))
                    return new CaseInsensitiveDictionaryStringObjectJsonConverter<IDictionary<string, object>>();
                if (typeToConvert == typeof(Dictionary<string, object>))
                    return new CaseInsensitiveDictionaryStringObjectJsonConverter<Dictionary<string, object>>();
                if (typeToConvert == typeof(IReadOnlyDictionary<string, object>))
                    return new CaseInsensitiveDictionaryStringObjectJsonConverter<IReadOnlyDictionary<string, object>>();
                if (typeToConvert == typeof(IEnumerable<KeyValuePair<string, object>>))
                    return new CaseInsensitiveDictionaryStringObjectJsonConverter<IEnumerable<KeyValuePair<string, object>>>();

                if (typeToConvert.TryGetSingleClosedGenericArguments(typeof(IDictionary<,>), out Type[] elementTypes)
                    || typeToConvert.TryGetSingleClosedGenericArguments(typeof(IReadOnlyDictionary<,>), out elementTypes)
                    || typeToConvert.TryGetSingleClosedGenericArguments(typeof(Dictionary<,>), out elementTypes)
                    || (typeToConvert.TryGetSingleClosedGenericArguments(typeof(IEnumerable<>), out Type[] enumerableTypes)
                        && enumerableTypes[0].TryGetSingleClosedGenericArguments(typeof(KeyValuePair<,>), out elementTypes)
                        && elementTypes[1] == typeof(object)))
                {
                    if (elementTypes[0] == typeof(string))
                    {
                        return (JsonConverter)(Activator.CreateInstance(typeof(CaseInsensitiveDictionaryJsonConverter<,>)
                            .MakeGenericType(typeToConvert, elementTypes[1])) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
                    }

                    if (elementTypes[0] == typeof(Uri))
                    {
                        return (JsonConverter)(Activator.CreateInstance(typeof(UriDictionarySystemTextJsonConverter<,>)
                            .MakeGenericType(typeToConvert, elementTypes[1])) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
                    }
                }
            }
        }

        if (IsConvertibleInterfaceType(typeToConvert))
        {
            return (JsonConverter)(Activator.CreateInstance(
                typeof(InterfaceJsonConverter<,>).MakeGenericType(typeToConvert, TypeMetadataCache.GetImplementationType(typeToConvert))) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
        }

        throw new ViciOneServiceBusException($"Unsupported type for json serialization {TypeCache.GetShortName(typeToConvert)}");
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
